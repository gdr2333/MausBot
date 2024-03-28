using EleCho.GoCqHttpSdk;
using EleCho.GoCqHttpSdk.Message;
using EleCho.GoCqHttpSdk.Post;
using MausBot.PluginInterface;

namespace MausBot.PluginSDK
{
    /// <summary>
    /// 通用指令实现
    /// </summary>
    public class Command : ICommand
    {
        /// <summary>
        /// <para>上下文作用域</para>
        /// <para><see cref="ICommand.Priority"/></para>
        /// </summary>
        public required ICommand.ContextScope Scope { get; init; }
        /// <summary>
        /// <para>优先级</para>
        /// <para><see cref="ICommand.Priority"/></para>
        /// </summary>
        public required int Priority { get; init; }
        /// <summary>
        /// <para>指令名称</para>
        /// <para><see cref="ICommand.Name"/></para>
        /// </summary>
        public required string Name { get; init; }
        /// <summary>
        /// <para>处理程序</para>
        /// <para><see cref="ICommand.Handler(IContext)"/></para>
        /// </summary>
        public required Func<IContext, Task> RealHandler { get; init; }
        /// <summary>
        /// <para>匹配程序</para>
        /// <para><see cref="ICommand.Matcher(CqMessage, string)"/></para>
        /// </summary>
        // 匿名函数比正则表达式好懂吧......吧？
        public required Func<CqMessage, string, bool> RealMatcher { get; init; }
        /// <summary>
        /// <see cref="ICommand.Handler(IContext)"/>
        /// </summary>
        public Task Handler(IContext context) => RealHandler(context);
        /// <summary>
        /// <see cref="ICommand.Matcher(CqMessage, string)"/>
        /// </summary>
        public bool Matcher(CqMessage message, string promptWord) => RealMatcher(message, promptWord);
    }
    /// <summary>
    /// 通用指令实现——泛型指令
    /// </summary>
    /// <typeparam name="T">要捕获的消息类型</typeparam>
    public class Command<T> : ICommand<T>
        where T : CqPostContext
    {
        /// <summary>
        /// <para>优先级</para>
        /// <para><see cref="ICommand{T}.Priority"/></para>
        /// </summary>
        public required int Priority { get; init; }
        /// <summary>
        /// <para>处理程序</para>
        /// <para><see cref="ICommand{T}.Handler(T, ICqActionSession)"/></para>
        /// </summary>
        public required Func<T, ICqActionSession, Task> RealHandler { get; init; }
        /// <summary>
        /// <para>匹配程序</para>
        /// <para><see cref="ICommand{T}.Matcher(T, string)"/></para>
        /// </summary>
        public required Func<T, string, bool> RealMatcher { get; init; }
        /// <summary>
        /// <see cref="ICommand{T}.Handler(T, ICqActionSession)"/>
        /// </summary>
        public Task Handler(T message, ICqActionSession session) => RealHandler(message, session);
        /// <summary>
        /// <see cref="ICommand{T}.Matcher(T, string)"/>
        /// </summary>
        public bool Matcher(T message, string promptWord) => RealMatcher(message, promptWord);
    }
}
