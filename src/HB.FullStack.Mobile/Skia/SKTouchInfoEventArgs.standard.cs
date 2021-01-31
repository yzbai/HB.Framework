﻿using SkiaSharp;

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace HB.FullStack.Mobile.Skia
{

    public class SKTouchInfoEventArgs : EventArgs
    {
        /// <summary>
        /// 第几个指头
        /// </summary>
        public long TouchEventId { get; set; }

        public bool IsOver { get; set; }

        /// <summary>
        /// 未经Figure的Matrix和Pivot转换的原始地址
        /// </summary>
        public SKPoint StartPoint { get; set; }

        /// <summary>
        /// 未经Figure的Matrix和Pivot转换的原始地址
        /// </summary>
        public SKPoint PreviousPoint { get; set; }

        /// <summary>
        /// 未经Figure的Matrix和Pivot转换的原始地址
        /// </summary>
        public SKPoint CurrentPoint { get; set; }

        /// <summary>
        /// 两个指头，不动的那个指头为支点
        /// </summary>
        public SKPoint PivotPoint { get; set; }

        public bool LongPressHappend { get; set; }

        public Task? LongPressTask { get; set; }
    }
}
