using System.Drawing;

namespace VncSharp
{
    /// <summary>
    /// A clipped version of VncDesktopTransformPolicy.
    /// </summary>
    public sealed class VncWpfDesktopPolicy : VncDesktopTransformPolicy
    {
        public VncWpfDesktopPolicy(VncClient vnc,
            RemoteDesktopWpf remoteDesktopWpf)
            : base(vnc, remoteDesktopWpf)
        {
        }

        public override bool AutoScroll
        {
            get
            {
                return true;
            }
        }

        public override Size AutoScrollMinSize
        {
            get
            {
                if (vnc != null && vnc.Framebuffer != null)
                {
                    return new Size(vnc.Framebuffer.Width, vnc.Framebuffer.Height);
                }
                else
                {
                    return new Size(100, 100);
                }
            }
        }

        public override Point UpdateRemotePointer(Point current)
        {
            Point adjusted = new Point();

            adjusted.X = (int) ((double) current.X/remoteDesktopWpf.ImageScale);
            adjusted.Y = (int)((double)current.Y / remoteDesktopWpf.ImageScale);

            return adjusted;
        }

        public override Rectangle AdjustUpdateRectangle(Rectangle updateRectangle)
        {
            int x, y;


            if (remoteDesktopWpf.ActualWidth > remoteDesktopWpf.designModeDesktop.ActualWidth)
            {
                x = updateRectangle.X +
                    (int)(remoteDesktopWpf.ActualWidth - remoteDesktopWpf.designModeDesktop.ActualWidth) / 2;
            }
            else
            {
                x = updateRectangle.X;
            }

            if (remoteDesktopWpf.ActualHeight > remoteDesktopWpf.designModeDesktop.ActualHeight)
            {
                y = updateRectangle.Y +
                    (int)(remoteDesktopWpf.ActualHeight - remoteDesktopWpf.designModeDesktop.ActualHeight) / 2;
            }
            else
            {
                y = updateRectangle.Y;
            }

            return new Rectangle(x, y, updateRectangle.Width, updateRectangle.Height);
        }

        public override Rectangle RepositionImage(Image desktopImage)
        {
            throw new System.NotImplementedException();
        }

        public override Rectangle GetMouseMoveRectangle()
        {
            throw new System.NotImplementedException();
        }

        public override Point GetMouseMovePoint(Point current)
        {
            return UpdateRemotePointer(current);
        }
    }
}