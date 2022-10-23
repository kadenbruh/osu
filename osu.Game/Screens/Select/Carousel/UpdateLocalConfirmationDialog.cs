// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Sprites;
using osu.Game.Overlays.Dialog;
using osu.Game.Localisation;

namespace osu.Game.Screens.Select.Carousel
{
    public class UpdateLocalConfirmationDialog : DeleteConfirmationDialog
    {
        public UpdateLocalConfirmationDialog(Action onConfirm)
        {
            HeaderText = PopupDialogStrings.UpdateLocallyModifiedText;
            BodyText = PopupDialogStrings.UpdateLocallyModifiedDescription;
            Icon = FontAwesome.Solid.ExclamationTriangle;
            DeleteAction = onConfirm;
        }
    }
}
