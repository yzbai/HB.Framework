﻿using System;
using System.Collections.Generic;
using System.Text;
using HB.FullStack.Mobile.Base;

namespace HB.FullStack.Mobile.Skia
{
    public abstract class BaseSkiaContentView : BaseContentView
    {
        public override void OnAppearing()
        {
            base.OnAppearing();

            ReAddFigures();
        }

        public override void OnDisappearing()
        {
            base.OnDisappearing();

            RemoveFigures();
        }

        protected abstract void RemoveFigures();

        protected abstract void ReAddFigures();
    }
}