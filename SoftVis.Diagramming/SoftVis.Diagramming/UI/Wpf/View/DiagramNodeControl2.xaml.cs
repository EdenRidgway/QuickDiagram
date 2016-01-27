﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Codartis.SoftVis.UI.Wpf.View
{
    /// <summary>
    /// Interaction logic for DiagramNodeControl2.xaml
    /// </summary>
    public partial class DiagramNodeControl2 : UserControl
    {
        public static readonly DependencyProperty DiagramFillProperty =
            DiagramVisual.DiagramFillProperty.AddOwner(typeof(DiagramNodeControl2));

        public static readonly DependencyProperty DiagramStrokeProperty =
            DiagramVisual.DiagramStrokeProperty.AddOwner(typeof(DiagramNodeControl2));

        public static readonly DependencyProperty FocusCommandProperty =
            DependencyProperty.Register("FocusCommand", typeof(ICommand), typeof(DiagramNodeControl2));

        public static readonly DependencyProperty UnfocusCommandProperty =
            DependencyProperty.Register("UnfocusCommand", typeof(ICommand), typeof(DiagramNodeControl2));

        public DiagramNodeControl2()
        {
            InitializeComponent();
            Focusable = true;
        }

        public Brush DiagramFill
        {
            get { return (Brush)GetValue(DiagramFillProperty); }
            set { SetValue(DiagramFillProperty, value); }
        }

        public Brush DiagramStroke
        {
            get { return (Brush)GetValue(DiagramStrokeProperty); }
            set { SetValue(DiagramStrokeProperty, value); }
        }

        public ICommand FocusCommand
        {
            get { return (ICommand)GetValue(FocusCommandProperty); }
            set { SetValue(FocusCommandProperty, value); }
        }

        public ICommand UnfocusCommand
        {
            get { return (ICommand)GetValue(UnfocusCommandProperty); }
            set { SetValue(UnfocusCommandProperty, value); }
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }
    }
}