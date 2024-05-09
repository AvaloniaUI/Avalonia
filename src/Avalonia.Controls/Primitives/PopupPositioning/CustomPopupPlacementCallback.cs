namespace Avalonia.Controls.Primitives.PopupPositioning;

/// <summary>
/// Represents a method that provides custom positioning for a <see cref="Popup"/> control.
/// </summary>
/// <param name="popupSize">The <see cref="Size"/> of the <see cref="Popup"/> control.</param>
/// <param name="targetRect">The <see cref="Rect"/> of the <see cref="Popup.PlacementTarget"/>.</param>
/// <param name="offset">The <see cref="Point"/> computed from the <see cref="Popup.HorizontalOffset"/> and <see cref="Popup.VerticalOffset"/> property values.</param>
public delegate CustomPopupPlacement CustomPopupPlacementCallback(Size popupSize, Rect targetRect, Point offset);
