﻿using OpenSage.Gui.Apt;
using OpenSage.Gui.Apt.ActionScript;

namespace OpenSage.Mods.Bfme.Gui
{
    [AptCallbacks(SageGame.Bfme)]
    static class MpGameSetup
    {
        // Called after the initialization has been performed
        public static void OnReadyPress(string param, ActionContext context, AptWindow window, Game game)
        {
        }
    }
}
