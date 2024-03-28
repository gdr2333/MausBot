using MausBot.PluginInterface;
using MausBot.PluginSDK;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace MausBot.Plugin.Internel;

public class AdminPlugin : IPlugin
{
    public string Name => "";

    public string Description => "";

    public Dictionary<string, string> CommandManual { get; } = new();

    public ICommand[] Commands { get; }

    public object[]? OtherCommands => null;

    public ILogger? Logger { get; set; }

    public AdminPlugin(List<long> adminList)
    {
        CommandManual.Add("请求管理权限", "请求管理员权限（需要能看到终端）");
        CommandManual.Add("添加管理员", "将指定用户添加为管理员");
        CommandManual.Add("删除管理员", "将指定用户从管理员列表中删除");
        Commands = [
            new Command(){
                Scope = ICommand.ContextScope.SameUser,
                Priority = 0,
                Name = "请求管理权限",
                RealMatcher = (message, promptWord) => message.Text.TrimEnd() == $"{promptWord}请求管理权限",
                RealHandler = async (context) =>
                {
                    _ = await context.ReadMessageAsync();
                    if(adminList.Contains(context.SenderUid))
                    {
                        await context.SendMessageAsync(new("您已经是管理员了"));
                        return;
                    }
                    else
                    {
                        var capchaArr = new byte[6];
                        Random.Shared.NextBytes(capchaArr);
                        var capcha = Convert.ToBase64String(capchaArr);
                        Logger.LogInformation($"管理员验证码是{capcha}，两分钟内有效");
                        var tStart = DateTime.UtcNow;
                        var res = await context.ReadMessageAsync();
                        if(DateTime.UtcNow - tStart <= TimeSpan.FromMinutes(2) && res.Message.Text == capcha)
                        {
                            await context.SendMessageAsync(new("您已成为机器人管理员！"));
                            Logger.LogInformation($"{context.SenderUid}现在是管理员");
                            lock(adminList)
                                adminList.Add(context.SenderUid);
                            return;
                        }
                        else
                        {
                            await context.SendMessageAsync(new("超时或验证码错误，请重试。"));
                            return;
                        }
                    }
                }
            },
            new Command()
            {
                Scope = ICommand.ContextScope.AdminOnly,
                Priority = 0,
                Name = "添加管理员",
                RealMatcher = (message, promptWord) => message.Text.StartsWith($"{promptWord}添加管理员"),
                RealHandler = async (context) =>
                {
                    var msg = await context.ReadMessageAsync();
                    if(long.TryParse(Regex.Match(msg.Message.Text,"""\d+$""").Value, out var adminId))
                    {
                        lock(adminList)
                            adminList.Add(adminId);
                        Logger.LogInformation($"{adminId}现在是管理员");
                        await context.SendMessageAsync(new($"{adminId}已经添加到管理员列表"));
                        return;
                    }
                    else
                        await context.SendMessageAsync(new($"输入无效，请输入一个正确的用户ID"));
                }
            },
            new Command()
            {
                Scope = ICommand.ContextScope.AdminOnly,
                Priority = 0,
                Name = "删除管理员",
                RealMatcher = (message,promptWord) => message.Text.StartsWith($"{promptWord}删除管理员"),
                RealHandler = async (context) =>
                {
                    var msg = await context.ReadMessageAsync();
                    if(long.TryParse(Regex.Match(msg.Message.Text,"""\d+$""").Value, out var adminId))
                    {
                        if(adminList.Contains(adminId))
                        {
                            lock(adminList)
                                adminList.Remove(adminId);
                            await context.SendMessageAsync(new($"用户{adminId}已从管理员列表中移除。\n请注意：该用户占有的具有管理员权限的上下文将不会立即关闭，这可能是安全隐患。如果需要，请重新启动机器人。"));
                            Logger.LogInformation($"{adminId}不再是管理员");
                        }
                        else
                            await context.SendMessageAsync(new($"输入无效：你输入的用户{adminId}当前不是管理员。"));
                        return;
                    }
                    else
                        await context.SendMessageAsync(new($"输入无效，请输入一个正确的用户ID"));
                }
            }
            ];
    }
}
