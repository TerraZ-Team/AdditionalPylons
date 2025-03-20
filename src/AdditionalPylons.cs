using System;
using Terraria;
using TerrariaApi.Server;

namespace AdditionalPylons
{
    [ApiVersion(2, 1)]
    public class AdditionalPylons : TerrariaPlugin
    {
        public override string Name => "AdditionalPylons";
        public override Version Version => System.Reflection.Assembly.GetAssembly(typeof(AdditionalPylons)).GetName().Version;
        public override string Author => "Stealownz";
        public override string Description => "You must construct additional pylons";
        public AdditionalPylons(Main game) : base(game) { }

        public override void Initialize() =>
            TShockEventsHandler.RegisterHandlers();

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
                TShockEventsHandler.UnrgisterHandlers();
            base.Dispose(isDisposing);
        }
    }
}
