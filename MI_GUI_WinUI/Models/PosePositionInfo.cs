using System;
using System.Collections.Generic;
using Windows.Foundation;

namespace MI_GUI_WinUI.Models;

public class PosePositionInfo
{
    public PoseGuiElement Pose { get; set; }
    public Point Position { get; set; }
    public Size Size { get; set; }

    public PosePositionInfo Clone()
    {
        return new PosePositionInfo
        {
            Pose = new PoseGuiElement
            {
                File = this.Pose.File,
                LeftSkin = this.Pose.LeftSkin,
                RightSkin = this.Pose.RightSkin,
                Sensitivity = this.Pose.Sensitivity,
                Deadzone = this.Pose.Deadzone,
                Linear = this.Pose.Linear,
                Flag = this.Pose.Flag,
                Landmark = this.Pose.Landmark,
                Position = this.Pose.Position != null ? new List<int>(this.Pose.Position) : null,
                Radius = this.Pose.Radius,
                Skin = this.Pose.Skin,
                Action = this.Pose.Action
            },
            Position = new Point(this.Position.X, this.Position.Y),
            Size = new Size(this.Size.Width, this.Size.Height)
        };
    }
}
