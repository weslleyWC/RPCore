namespace RPCore
{
    public static class Notify
    {
        public static void Show(string text) { GTA.UI.Notification.PostTicker(text, false, false); }
    }
}
