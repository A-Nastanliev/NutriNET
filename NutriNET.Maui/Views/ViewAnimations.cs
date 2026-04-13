using System;
using System.Collections.Generic;
using System.Text;

namespace NutriNET.Maui.Views
{
    public static class ViewAnimations
    {
        public static Task AnimateBottomMargin( this View view, double from, double to, uint duration, Easing? easing = null)
        {
            var tcs = new TaskCompletionSource();

            var animation = new Animation(v =>
            {
                view.Margin = new Thickness(0, 0, 0, v);
            }, from, to);

            animation.Commit(view, "MarginAnimation", 16, duration, easing ?? Easing.SinOut,(v, c) => tcs.SetResult());

            return tcs.Task;
        }
    }
}
