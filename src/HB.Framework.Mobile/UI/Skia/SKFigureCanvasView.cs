﻿using HB.Framework.Client.Base;
using HB.Framework.Client.Effects;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using Xamarin.Forms;
using System.Linq;
using AsyncAwaitBestPractices;
using System.Threading;
using System.Diagnostics.CodeAnalysis;

namespace HB.Framework.Client.UI.Skia
{
    [SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable",
        Justification = "当Page Disappearing时，会调用所有BaseContentView的Disappering。那里会dispose")]
    public class SKFigureCanvasView : SKCanvasView, IBaseContentView
    {
        public static readonly BindableProperty FiguresProperty = BindableProperty.Create(nameof(Figures), typeof(ObservableCollection<SKFigure>), typeof(SKFigureCanvasView), new ObservableCollection<SKFigure>(), propertyChanged: (b, o, n) => { ((SKFigureCanvasView)b).OnFiguresChanged((ObservableCollection<SKFigure>?)o, (ObservableCollection<SKFigure>?)n); });
        public static readonly BindableProperty IsAnimationModeProperty = BindableProperty.Create(nameof(IsAnimationMode), typeof(bool), typeof(SKFigureCanvasView), false, propertyChanged: (b, o, n) => { ((SKFigureCanvasView)b).OnIsAnimationModeChanged((bool)o, (bool)n); });
        public static readonly BindableProperty AnimationIntervalProperty = BindableProperty.Create(nameof(AnimationInterval), typeof(int), typeof(SKFigureCanvasView), 16, propertyChanged: (b, o, n) => { ((SKFigureCanvasView)b).OnAnimationIntervalChanged(); });

        private readonly WeakEventManager _eventManager = new WeakEventManager();
        private readonly Dictionary<long, SKFigure> _touchDictionary = new Dictionary<long, SKFigure>();
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private Timer? _animationTimer;

        public SKFigureCanvasView() : base()
        {
            TouchEffect touchEffect = new TouchEffect { Capture = true };

            touchEffect.TouchAction += TouchEffect_TouchAction;

            Effects.Add(touchEffect);

            PaintSurface += FigureCanvasView_PaintSurface;
        }

        public ObservableCollection<SKFigure> Figures { get => (ObservableCollection<SKFigure>)GetValue(FiguresProperty); private set => SetValue(FiguresProperty, value); }

        public bool IsAnimationMode { get => (bool)GetValue(IsAnimationModeProperty); set => SetValue(IsAnimationModeProperty, value); }

        public int AnimationInterval { get => (int)GetValue(AnimationIntervalProperty); set => SetValue(AnimationIntervalProperty, value); }

        public long ElapsedMilliseconds { get => _stopwatch.ElapsedMilliseconds; }

        public bool IsAnimating { get; private set; }

        public bool IsAppearing { get; private set; }

        public bool AutoBringToFront { get; set; }

        public bool EnableFailedToHitEvent { get; set; } = true;

        public event EventHandler<SKPaintSurfaceEventArgs> Painting
        {
            add => _eventManager.AddEventHandler(value);
            remove => _eventManager.RemoveEventHandler(value);
        }

        public event EventHandler<SKPaintSurfaceEventArgs> Painted
        {
            add => _eventManager.AddEventHandler(value);
            remove => _eventManager.AddEventHandler(value);
        }

        public void OnAppearing()
        {
            IsAppearing = true;

            if (IsAnimationMode)
            {
                ReStartAnimation();
            }
        }

        public void OnDisappearing()
        {
            IsAppearing = false;

            StopAnimation();

            _touchDictionary.Clear();
        }

        public IList<IBaseContentView?>? GetAllCustomerControls()
        {
            return null;
        }

        private void OnIsAnimationModeChanged(bool oldValue, bool newValue)
        {
            if (oldValue == newValue)
            {
                return;
            }

            if (newValue && IsAppearing)
            {
                ReStartAnimation();
            }
            else
            {
                StopAnimation();
            }
        }

        private void OnAnimationIntervalChanged()
        {
            ReStartAnimation();
        }

        private void OnFiguresChanged(ObservableCollection<SKFigure>? oldValue, ObservableCollection<SKFigure>? newValue)
        {
            StopAnimation();

            oldValue.ForEach(f => f.Dispose());
            newValue.ForEach(f => f.Parent = this);

            if (IsAnimationMode)
            {
                ReStartAnimation();
            }
            else
            {
                InvalidateSurface();
            }

            if (oldValue is ObservableCollection<SKFigure> oldCollection)
            {
                oldCollection.CollectionChanged -= OnFiguresCollectionChanged;
            }

            if (newValue is ObservableCollection<SKFigure> newCollection)
            {
                newCollection.CollectionChanged += OnFiguresCollectionChanged;
            }
        }

        private void OnFiguresCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (sender is IEnumerable<SKFigure> figures)
            {
                figures.ForEach(f => f.Parent = this);
            }

            if (IsAnimationMode)
            {
                ReStartAnimation();
            }
            else
            {
                InvalidateSurface();
            }
        }

        private void ReStartAnimation()
        {
            if (!IsAnimationMode)
            {
                return;
            }

            _stopwatch.Restart();
            IsAnimating = true;

            if (_animationTimer == null)
            {
                _animationTimer = new Timer(
                    new TimerCallback(state =>
                    {
                        Device.BeginInvokeOnMainThread(() => InvalidateSurface());
                    }),
                    null,
                    0,
                    AnimationInterval);
            }
            else
            {
                _animationTimer.Change(0, AnimationInterval);
            }
        }

        private void StopAnimation()
        {
            _animationTimer?.Dispose();
            _animationTimer = null;

            IsAnimating = false;
            _stopwatch.Stop();
        }

        private void FigureCanvasView_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            SKCanvas canvas = e.Surface.Canvas;

            canvas.Clear();

            OnPainting(sender, e);

            OnPaintFigures(e, canvas);

            OnPainted(sender, e);
        }

        private void OnPainting(object sender, SKPaintSurfaceEventArgs e)
        {
            SKCanvas canvas = e.Surface.Canvas;

            using (new SKAutoCanvasRestore(canvas))
            {
                _eventManager.RaiseEvent(sender, e, nameof(Painting));
            }
        }

        private void OnPaintFigures(SKPaintSurfaceEventArgs e, SKCanvas canvas)
        {
            Figures.ForEach(f =>
            {
                using (new SKAutoCanvasRestore(canvas))
                {
                    f.Paint(e);
                }
            });
        }

        private void OnPainted(object sender, SKPaintSurfaceEventArgs e)
        {
            SKCanvas canvas = e.Surface.Canvas;

            using (new SKAutoCanvasRestore(canvas))
            {
                _eventManager.RaiseEvent(sender, e, nameof(Painted));
            }
        }

        private void TouchEffect_TouchAction(object sender, TouchActionEventArgs args)
        {
            if (Figures == null)
            {
                return;
            }

            SKPoint skPoint = SKUtil.ToSKPoint(args.Location);

            long eventId = args.Id;

            SKFigure? relatedFigure = null;

            if (_touchDictionary.ContainsKey(eventId))
            {
                relatedFigure = _touchDictionary[eventId];
            }

            switch (args.Type)
            {
                case TouchActionType.Pressed:

                    if (relatedFigure != null)
                    {
                        _touchDictionary.Remove(eventId);

                        return;
                    }

                    bool founded = false;

                    for (int i = Figures.Count - 1; i >= 0; --i)
                    {
                        SKFigure figure = Figures[i];

                        if (!founded && figure.HitTest(skPoint, args.Id))
                        {
                            founded = true;

                            _touchDictionary.Add(eventId, figure);

                            figure.ProcessTouchAction(args);

                            if (AutoBringToFront)
                            {
                                if (Figures.Remove(figure))
                                {
                                    Figures.Add(figure);
                                }
                            }

                            if (!EnableFailedToHitEvent)
                            {
                                break;
                            }
                        }
                        else
                        {
                            if (EnableFailedToHitEvent)
                            {
                                TouchActionEventArgs unTouchArgs = new TouchActionEventArgs(args.Id, TouchActionType.HitFailed, args.Location, args.IsInContact);
                                figure.ProcessTouchAction(unTouchArgs);
                            }
                        }
                    }

                    if (!IsAnimationMode)
                    {
                        InvalidateSurface();
                    }

                    break;
                case TouchActionType.Moved:

                    if (relatedFigure != null)
                    {
                        relatedFigure.ProcessTouchAction(args);

                        if (!IsAnimationMode)
                        {
                            InvalidateSurface();
                        }
                    }
                    break;
                case TouchActionType.Released:
                case TouchActionType.Exited:
                case TouchActionType.Cancelled:
                    if (relatedFigure != null)
                    {
                        relatedFigure.ProcessTouchAction(args);

                        _touchDictionary.Remove(eventId);

                        if (!IsAnimationMode)
                        {
                            InvalidateSurface();
                        }
                    }

                    break;
            }
        }
    }
}