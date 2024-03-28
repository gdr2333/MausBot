using EleCho.GoCqHttpSdk;
using EleCho.GoCqHttpSdk.Action;
using EleCho.GoCqHttpSdk.Message;
using EleCho.GoCqHttpSdk.Post;
using Microsoft.Extensions.Logging;

namespace MausBot.PluginInterface;

/// <summary>
/// 对话文本上下文实现
/// </summary>
public interface IContext : IEquatable<IContext>
{
    /// <summary>
    /// 当前机器人Uid，好像没什么用的样子（
    /// </summary>
    public long BotUid { get; }
    /// <summary>
    /// 当前群聊id，对于私聊消息为-1
    /// </summary>
    public long GroupId { get; }
    /// <summary>
    /// 消息发送者Uid，对于上下文范围是全群的消息为-1
    /// </summary>
    public long SenderUid { get; }
    /// <summary>
    /// 获取下一条消息
    /// </summary>
    /// <returns>当前上下文的下一条消息</returns>
    /// <exception cref="InvalidOperationException">在停用的上下文中调用</exception>
    public Task<CqMessagePostContext> ReadMessageAsync();
    /// <summary>
    /// 在当前上下文发送一条消息
    /// </summary>
    /// <param name="message">要发送的消息</param>
    /// <exception cref="InvalidOperationException">在停用的上下文中调用</exception>
    public Task<CqSendMessageActionResult?> SendMessageAsync(CqMessage message);
    /// <summary>
    /// 指示当前上下文是否为活跃状态。请注意：如果操作者发起了强制卸载上下文的请求，IsActive也会为false。
    /// </summary>
    public bool IsActive { get; }
}

/// <summary>
/// 捕获非文本消息或全局消息的命令
/// </summary>
/// <typeparam name="T">要捕获的消息类型</typeparam>
public interface ICommand<T>
    where T : CqPostContext
{
    /// <summary>
    /// 优先级
    /// </summary>
    /// <remarks>别TM给我写<c>int.MaxValue</c>，听见没？？？</remarks>
    public int Priority { get; }
    /// <summary>
    /// 检测
    /// </summary>
    /// <param name="message">收到的消息</param>
    /// <param name="promptWord">当前设置的提示词</param>
    /// <returns>是否处理该消息</returns>
    /// <remarks>请不要给处理过程写到这个函数里，谢谢</remarks>
    public bool Matcher(T message, string promptWord);
    /// <summary>
    /// 消息处理
    /// </summary>
    /// <param name="message">收到的消息</param>
    /// <param name="session">当前会话</param>
    public Task Handler(T message,ICqActionSession session);
}

/// <summary>
/// 用于处理常见指令的类（包括上下文管理）
/// </summary>
public interface ICommand
{
    /// <summary>
    /// 上下文范围（枚举定义）
    /// </summary>
    public enum ContextScope {
        /// <summary>
        /// 仅限管理员私聊
        /// </summary>
        AdminPrivateOnly,
        /// <summary>
        /// 仅限私聊
        /// </summary>
        PrivateOnly,
        /// <summary>
        /// 仅限同一管理员
        /// </summary>
        AdminOnly,
        /// <summary>
        /// 仅限同一用户
        /// </summary>
        SameUser,
        /// <summary>
        /// 只要在一个群里就行
        /// </summary>
        SameGroup };
    /// <summary>
    /// 上下文范围
    /// </summary>
    public ContextScope Scope { get; }
    /// <summary>
    /// 优先级
    /// </summary>
    /// <remarks>别TM给我写<c>int.MaxValue</c>，听见没？？？</remarks>
    public int Priority { get; }
    /// <summary>
    /// 检测
    /// </summary>
    /// <param name="message">收到的消息</param>
    /// <param name="promptWord">当前设置的提示词</param>
    /// <returns>是否处理该消息</returns>
    /// <remarks>请不要给处理过程写到这个函数里，谢谢</remarks>
    public bool Matcher(CqMessage message, string promptWord);
    /// <summary>
    /// 处理
    /// </summary>
    /// <param name="context">当前上下文</param>
    public Task Handler(IContext context);
    /// <summary>
    /// 指令名称
    /// </summary>
    public string Name { get; }
}

/// <summary>
/// 插件类
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// 插件名称
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// 插件描述
    /// </summary>
    public string Description { get; }
    /// <summary>
    /// 指令手册
    /// </summary>
    public Dictionary<string,string> CommandManual { get; }
    /// <summary>
    /// 继承自ICommand的指令
    /// </summary>
    public ICommand[] Commands { get; }
    /// <summary>
    /// 继承自<c>ICommand&lt;T&gt;</c>的指令
    /// </summary>
    /// <remarks>别往里面塞奇奇怪怪的东西，未定义行为警告</remarks>
    public object[]? OtherCommands { get; }
    /// <summary>
    /// <para>给你写日志的</para>
    /// <para><see href="https://learn.microsoft.com/zh-cn/dotnet/core/extensions/logging"/></para>
    /// </summary>
    // 初始化过程之后这玩意就不是null了，信我。
    public ILogger? Logger { set; }
}