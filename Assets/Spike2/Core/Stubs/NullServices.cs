namespace Spike2.Core.Stubs
{
    /// <summary>Registers the null implementation of every contract. Called once by Services.</summary>
    public static class NullServices
    {
        public static void RegisterAll()
        {
            Services.RegisterNull<IPlayer>(new NullPlayer());
            Services.RegisterNull<IPocket>(new NullPocket());
            Services.RegisterNull<IThrowables>(new NullThrowables());
            Services.RegisterNull<IClipboard>(new NullClipboard());
            Services.RegisterNull<IPanelSystem>(new NullPanelSystem());
            Services.RegisterNull<IZatan>(new NullZatan());
            Services.RegisterNull<IZatanDirector>(new NullZatanDirector());
            Services.RegisterNull<ICameraNetwork>(new NullCameraNetwork());
            Services.RegisterNull<IAnnouncer>(new NullAnnouncer());
            Services.RegisterNull<IAmbushWarning>(new NullAmbushWarning());
            Services.RegisterNull<ICaptureDirector>(new NullCaptureDirector());
            Services.RegisterNullFactory<IDoor>(id => new NullDoor(id));
            Services.RegisterNullFactory<IMachine>(id => Services.Get<IPanelSystem>().Get(id));
        }

        internal static void Log(string msg) => UnityEngine.Debug.Log("[stub] " + msg);
    }
}
