﻿using System;
using System.Collections.Generic;
using System.Text;

namespace HB.FullStack.Client.Platforms
{
    public interface IPlatformStatusBarHelper
    {
        bool IsShowing { get; }

        void Show();

        void Hide();
    }
}
