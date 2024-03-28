using System.Text.Json;
using System.Text.Json.Serialization;

namespace MausBot.Data;

/// <summary>
/// 配置文件
/// </summary>
internal class Config
{
    /// <summary>
    /// 管理员列表
    /// </summary>
    public List<long> AdminList { get; set; } = [];
    /// <summary>
    /// 是否使用反向websocket，为否则使用正向websocket
    /// </summary>
    public bool UseReverseLink { get; set; } = true;
    /// <summary>
    /// websocket地址
    /// </summary>
    public Uri Address { get; set; } = new("ws://localhost");
    /// <summary>
    /// 插件地址
    /// </summary>
    public string[] PluginAddress { get; set; } = [];
}