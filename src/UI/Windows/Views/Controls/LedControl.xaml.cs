using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace OSDPBench.Windows.Views.Controls;

public partial class LedControl
{
    #region Dependency Properties

    public static readonly DependencyProperty LedColorProperty = DependencyProperty.Register(
        nameof(LedColor), 
        typeof(Color), 
        typeof(LedControl), 
        new PropertyMetadata(Colors.Red, OnLedColorChanged));

    public static readonly DependencyProperty LastActivityTimeProperty = DependencyProperty.Register(
        nameof(LastActivityTime), 
        typeof(DateTime), 
        typeof(LedControl), 
        new PropertyMetadata(DateTime.MinValue, OnLastActivityTimeChanged));

    #endregion

    #region Properties

    public Color LedColor
    {
        get => (Color)GetValue(LedColorProperty);
        set => SetValue(LedColorProperty, value);
    }

    public DateTime LastActivityTime
    {
        get => (DateTime)GetValue(LastActivityTimeProperty);
        set => SetValue(LastActivityTimeProperty, value);
    }

    #endregion
    
    private bool _isActive;

    public LedControl()
    {
        InitializeComponent();
        UpdateLedColor();
    }

    #region Property Change Handlers

    private static void OnLedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LedControl control)
        {
            control.UpdateLedColor();
        }
    }

    private static void OnLastActivityTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LedControl control)
        {
            control.Pulse();
        }
    }

    #endregion

    #region Update Methods

    private void UpdateLedColor()
    {
        Led.Fill = new SolidColorBrush(LedColor);
        Led.Opacity = 0.3;
        if (Led.Effect is DropShadowEffect effect)
        {
            effect.Color = LedColor;
            effect.Opacity = 0;
        }
    }
    
    #endregion

    #region Pulse Methods

    private void Pulse()
    {
        if (_isActive) return;
        _isActive = true;
        
        // Single pulse animation
        var pulseAnimation = new DoubleAnimation
        {
            From = 0.3,
            To = 1.0,
            Duration = TimeSpan.FromSeconds(0.1),
            AutoReverse = true
        };
        
        // Store the effect reference for use in the completed callback
        if (Led.Effect is DropShadowEffect effect)
        {
            effect.Color = LedColor;
            effect.Opacity = 1.0;

            // Reset to normal state after animation completes
            pulseAnimation.Completed += (_, _) =>
            {
                _isActive = false;

                effect.Opacity = 0;
            };
        }

        Led.BeginAnimation(OpacityProperty, pulseAnimation);
    }

    #endregion
}