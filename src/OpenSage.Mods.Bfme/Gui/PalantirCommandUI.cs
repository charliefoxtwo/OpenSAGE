﻿using OpenSage.Gui.Apt;
using OpenSage.Gui.Apt.ActionScript;

namespace OpenSage.Mods.Bfme.Gui
{
    [AptCallbacks(SageGame.Bfme)]
    class PalantirCommandUI
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public static void OnSubMenuLoaded(string param, ActionContext context, AptWindow window, Game game)
        {
        }

        public static void OnButtonFrameLoaded(string param, ActionContext context, AptWindow window, Game game)
        {
        }

        public static void OnToggleFlashLoaded(string param, ActionContext context, AptWindow window, Game game)
        {
        }
    }
}
