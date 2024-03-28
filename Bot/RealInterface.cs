using EleCho.GoCqHttpSdk;
using EleCho.GoCqHttpSdk.Action;
using EleCho.GoCqHttpSdk.Message;
using EleCho.GoCqHttpSdk.Post;
using MausBot.PluginInterface;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using static MausBot.PluginInterface.ICommand;

namespace MausBot;

public class Context : IContext, IEquatable<Context>, IComparable<Context>
{
    public required long BotUid { get; init; }

    public required long GroupId { get; init; }

    public required long SenderUid { get; init; }

    public bool IsActive { get; set; } = true;

    public async Task<CqMessagePostContext> ReadMessageAsync()
    {
        CqMessagePostContext res;
        while (!MessageQueue.TryDequeue(out res) || IsActive)
            await Task.Delay(10);
        if (!IsActive)
            throw new InvalidOperationException("尝试在停用的上下文中读取信息");
        return res;
    }

    public async Task<CqSendMessageActionResult> SendMessageAsync(CqMessage message)
    {
        CqSendMessageActionResult? r = null;
        if (GroupId == -1)
            r = await Session.SendPrivateMessageAsync(SenderUid, message);
        else
            r = await Session.SendGroupMessageAsync(GroupId, message);
        if (r == null || r.Status == CqActionStatus.Failed)
            Logger.LogError($"向{ToInfoStr()}发送信息的操作返回了{r}，请检查连接状态。");
        else
            Logger.LogInformation($"向{ToInfoStr()}发送信息：{r.Status}");
        return r;
    }

    public required ICqActionSession Session { init; private get; }

    public required ILogger Logger { init; private get; }

    internal string ToInfoStr() =>
        $$"""{"BotUid":{{BotUid}},"GroupId":{{GroupId}},"SenderUid":{{SenderUid}}}""";

    public bool Equals(Context? other) =>
        BotUid == other?.BotUid && SenderUid == other?.SenderUid && GroupId == other?.GroupId;

    public bool Equals(IContext? other) =>
        BotUid == other?.BotUid && SenderUid == other?.SenderUid && GroupId == other?.GroupId;

    public required ConcurrentQueue<CqMessagePostContext> MessageQueue { get; init; }

    public required ContextScope Scope { get; init; }

    public override bool Equals(object obj) =>
        Equals(obj as Context);

    public int CompareTo(Context? other) =>
        Priority.CompareTo(other?.Priority ?? 0);

    public required int Priority { get; init; }
}

public class ContextManager : ICommand<CqMessagePostContext>
{
    //因为我要在这用int.MaxValue
    public int Priority { get; } = int.MaxValue;

    public required ILogger Logger { set; private get; }
    public required ILogger LoggerForContext { set; private get; }

    public async Task Handler(CqMessagePostContext message, ICqActionSession session)
    {
        lock (contexts)
        {
            foreach (var context in contexts)
                if (context.IsActive && Match(message, context))
                {
                    context.MessageQueue.Enqueue(message);
                    Logger.LogInformation($"已经为消息{message}找到了活跃的上下文{context.ToInfoStr()}并将消息发送到消息队列。");
                    return;
                }
        }
        foreach (var command in commands)
        {
            if (Match(message, command, PromptWord))
            {
                Context? context = null;
                if (message is CqGroupMessagePostContext groupMessage)
                    context = new()
                    {
                        BotUid = message.SelfId,
                        GroupId = groupMessage.GroupId,
                        SenderUid = command.Scope == ContextScope.SameGroup ? -1 : groupMessage.UserId,
                        Logger = factory.CreateLogger($"Context:{command.Name}"),
                        Session = (ICqActionSession)message.Session,
                        Scope = command.Scope,
                        Priority = command.Scope == ContextScope.SameGroup ? -1 : 0,
                        MessageQueue = new([message])
                    };
                else if (message is CqPrivateMessagePostContext privateMessage)
                    context = new()
                    {
                        BotUid = message.SelfId,
                        GroupId = -1,
                        SenderUid = command.Scope == ContextScope.SameGroup ? -1 : privateMessage.UserId,
                        Logger = factory.CreateLogger($"Context:{command.Name}"),
                        Session = (ICqActionSession)message.Session,
                        Scope = command.Scope,
                        Priority = command.Scope == ContextScope.SameGroup ? -1 : 0,
                        MessageQueue = new([message])
                    };
                if (context != null)
                {
                    Logger.LogInformation($"已经因消息 {message} 对指令 {command.Name} 创建了一个上下文 {context.ToInfoStr()}");
                    lock (contexts)
                    {
                        contexts.Add(context);
                        contexts.Sort();
                    }
                    Logger.LogInformation($"指令 {command.Name} 正在运行，使用上下文 {context.ToInfoStr()}");
                    await command.Handler(context);
                    Logger.LogInformation($"指令 {command.Name} 已退出");
                    context.IsActive = false;
                    lock (contexts)
                        contexts.Remove(context);
                    Logger.LogInformation($"上下文 {context} 已销毁");
                    return;
                }
            }

        }
        //why?
        Logger.LogWarning($"意料之外的情况：对可能匹配当前指令/上下文的信息{message}却没有找到任何指令或上下文。");
        return;
    }

    public bool Matcher(CqMessagePostContext message, string promptWord)
    {
        lock (contexts)
        {
            foreach (var context in contexts)
                if (context.IsActive && Match(message, context))
                {
                    Logger.LogInformation($"检测到消息{message}与活跃的上下文{context.ToInfoStr}匹配，已准备好处理");
                    return true;
                }
        }
        foreach (var command in commands)
        {
            if (Match(message, command, promptWord))
            {
                Logger.LogInformation($"检测到消息{message}与活跃的命令{command.Name}匹配，已准备好处理");
                return true;
            }
        }
        return false;
    }

    public ContextManager(ICommand[] Commands, List<long> AdminList)
    {
        adminList = AdminList;
        contexts = new();
        commands = Commands;
        Array.Sort(commands, new CommandComparer());
        factory = LoggerFactory.Create(builder => builder.AddConsole());
    }

    private bool Match(CqMessagePostContext message, ICommand command, string promptWord)
    {
        if (message is CqGroupMessagePostContext groupMessage)
            switch (command.Scope)
            {
                case ContextScope.AdminPrivateOnly:
                case ContextScope.PrivateOnly:
                    return false;
                case ContextScope.AdminOnly:
                    if (adminList.IndexOf(groupMessage.UserId) == -1)
                        return false;
                    goto default;
                default:
                    return command.Matcher(groupMessage.Message, promptWord);
            }
        else if (message is CqPrivateMessagePostContext privateMessage)
            switch (command.Scope)
            {
                case ContextScope.AdminPrivateOnly:
                    goto case ContextScope.AdminOnly;
                case ContextScope.PrivateOnly:
                    goto default;
                case ContextScope.AdminOnly:
                    if (adminList.IndexOf(privateMessage.UserId) == -1)
                        return false;
                    goto default;
                default:
                    return command.Matcher(privateMessage.Message, promptWord);
            }
        else
            //wtf?
            return false;
    }

    private static bool Match(CqMessagePostContext message, Context context)
    {
        if (message is CqPrivateMessagePostContext privateMessage)
            return context.GroupId == -1 && context.SenderUid == privateMessage.UserId;
        else if (message is CqGroupMessagePostContext groupMessage)
            return context.GroupId == groupMessage.GroupId && (context.SenderUid == groupMessage.UserId || context.SenderUid == -1);
        else
            //wtf?
            return false;
    }

    private List<long> adminList;
    private List<Context> contexts;
    private ICommand[] commands;
    public required string PromptWord { get; init; }
    private ILoggerFactory factory;
}

//我能预感到这几个玩意的IL代码会很炸裂
public class CommandComparer : Comparer<ICommand>
{
    public override int Compare(ICommand? x, ICommand? y)
    {
        return (x?.Priority ?? 0).CompareTo(y?.Priority ?? 0);
    }
}

public class CommandComparer<T> : Comparer<ICommand<T>>
    where T : CqPostContext
{
    public override int Compare(ICommand<T>? x, ICommand<T>? y)
    {
        return (x?.Priority ?? 0).CompareTo(y?.Priority ?? 0);
    }
}