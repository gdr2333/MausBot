using EleCho.GoCqHttpSdk;
using EleCho.GoCqHttpSdk.Post;
using MausBot.Data;
using MausBot.Plugin.Internel;
using MausBot.PluginInterface;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MausBot.Host;

public sealed class BotService : IHostedService
{
    private readonly ILogger _logger;
    Config config;
    CqSession session;
    JsonSerializerOptions jsonopt = new JsonSerializerOptions() { WriteIndented = true };
    public BotService(ILogger<BotService> logger)
    {
        _logger = logger;
        config = new();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("1. StartAsync has been called.");
        if (!File.Exists("config.json"))
        {
            _logger.LogCritical("配置文件不存在！请参照config_demo.json填写config.json并放置在程序目录下");
            Environment.Exit(1);
        }
        var conf = await JsonSerializer.DeserializeAsync<Config>(File.Open("config.json", FileMode.Open), cancellationToken: cancellationToken);
        if (conf == null)
        {
            _logger.LogCritical("配置文件不存在或格式错误：解析结果为空");
            Environment.Exit(1);
        }
        config = conf;

        //别看，全是废话
        List<IPlugin> plugins = new();
        List<ICommand> commands = new();
        List<ICommand<CqMessagePostContext>> commands_message = new();
        List<ICommand<CqGroupMessagePostContext>> commands_groupMessage = new();
        List<ICommand<CqPrivateMessagePostContext>> commands_privateMessage = new();
        //感谢IntelliCode
        List<ICommand<CqClientStatusChangedPostContext>> commands_clientStatusChanged = new();
        List<ICommand<CqFriendAddedPostContext>> commands_friendAddedPost = new();
        List<ICommand<CqFriendMessageRecalledPostContext>> commands_FriendMessageRecalled = new();
        List<ICommand<CqFriendRequestPostContext>> commands_friendRequestPost = new();
        List<ICommand<CqGroupAdministratorChangedPostContext>> commands_administratorChanged = new();
        List<ICommand<CqGroupEssenceChangedPostContext>> commands_groupEssenceChanged = new();
        List<ICommand<CqGroupFileUploadedPostContext>> commands_fileUploaded = new();
        List<ICommand<CqGroupMemberBanChangedPostContext>> commands_memberBanChanged = new();
        List<ICommand<CqGroupMemberDecreasedPostContext>> commands_memberDecreased = new();
        List<ICommand<CqGroupMemberHonorChangedPostContext>> commands_memberHonorChanged = new();
        List<ICommand<CqGroupMemberIncreasedPostContext>> commands_memberIncreased = new();
        List<ICommand<CqGroupMemberNicknameChangedPostContext>> commands_memberNicknameChanged = new();
        List<ICommand<CqGroupMemberTitleChangeNoticedPostContext>> commands_groupMemberTitleChangeNoticed = new();
        List<ICommand<CqGroupMessageRecalledPostContext>> commands_groupMessageRecalled = new();
        List<ICommand<CqGroupRequestPostContext>> commands_groupGroupRequest = new();
        List<ICommand<CqNoticePostContext>> commands_notice = new();
        List<ICommand<CqNotifyNoticePostContext>> command_notifyNotice = new();
        List<ICommand<CqRequestPostContext>> command_request = new();

        plugins.Add(new AdminPlugin(config.AdminList));

        if (config.UseReverseLink)
        {
            var session = new CqRWsSession(new() { BaseUri = config.Address });
            _logger.LogInformation($"正在连接反向ws会话：{config.Address}");
            try
            {
                await session.StartAsync();
            }
            catch (Exception e)
            {
                _logger.LogCritical($"连接失败：{e}");
                Environment.Exit(1);
            }
            _logger.LogInformation($"已连接到{config.Address}");
            
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("正在保存配置文件......");
        await JsonSerializer.SerializeAsync(File.Open("config.json", FileMode.Open), config, options: jsonopt, cancellationToken: cancellationToken);
        _logger.LogInformation("配置文件已保存。");
    }
}