using MessagePipe;
using VContainer;

namespace Code.Game.Main.Installers
{
    public static class MessagePipeInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            var options = builder.RegisterMessagePipe();
        
            builder.RegisterBuildCallback(c => GlobalMessagePipe.SetProvider(c.AsServiceProvider()));

            builder.RegisterMessageBroker<int>(options);
        }
    }
}