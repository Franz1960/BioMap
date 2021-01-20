using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Blazor.ImageSurveyor
{
    /// <summary>
    /// MapOptions object used to define the properties that can be set on a Map.
    /// </summary>
    public class ImageSurveyorOptions
    {
        /// <summary>
        /// Color used for the background of the Map div. 
        /// This color will be visible when tiles have not yet loaded as the user pans. 
        /// This option can only be set when the map is initialized.
        /// </summary>
        public string BackgroundColor { get; set; }

        /// <summary>
        /// When false, map icons are not clickable. 
        /// A map icon represents a point of interest, also known as a POI. 
        /// By default map icons are clickable.
        /// </summary>
        public bool? ClickableIcons { get; set; }

        /// <summary>
        /// Enables/disables all default UI. May be overridden individually.
        /// </summary>
        public bool? DisableDefaultUI { get; set; }

        /// <summary>
        /// Enables/disables zoom and center on double click. Enabled by default.
        /// </summary>
        public bool? DisableDoubleClickZoom { get; set; }

        /// <summary>
        /// If false, prevents the map from being dragged. Dragging is enabled by default.
        /// </summary>
        public bool? Draggable { get; set; }

        /// <summary>
        /// The name or url of the cursor to display when mousing over a draggable map. 
        /// This property uses the css cursor attribute to change the icon. 
        /// As with the css property, you must specify at least one fallback cursor that is not a URL. 
        /// For example: draggableCursor: 'url(http://www.example.com/icon.png), auto;'.
        /// </summary>
        public string DraggableCursor { get; set; }

        /// <summary>
        /// The name or url of the cursor to display when the map is being dragged. 
        /// This property uses the css cursor attribute to change the icon. 
        /// As with the css property, you must specify at least one fallback cursor that is not a URL. 
        /// For example: draggingCursor: 'url(http://www.example.com/icon.png), auto;'.
        /// </summary>
        public string DraggingCursor { get; set; }

        /// <summary>
        /// This setting controls how the API handles gestures on the map. Allowed values:
        /// "cooperative": Scroll events and one-finger touch gestures scroll the page, and do not zoom or pan the map. Two-finger touch gestures pan and zoom the map. Scroll events with a ctrl key or ⌘ key pressed zoom the map. In this mode the map cooperates with the page.
        /// "greedy": All touch gestures and scroll events pan or zoom the map.
        /// "none": The map cannot be panned or zoomed by user gestures.
        /// "auto": (default) Gesture handling is either cooperative or greedy, depending on whether the page is scrollable or in an iframe.
        /// </summary>
        public string GestureHandling { get; set; }

        /// <summary>
        /// The heading for aerial imagery in degrees measured clockwise from cardinal direction North. 
        /// Headings are snapped to the nearest available angle for which imagery is available.
        /// </summary>
        public int? Heading { get; set; }

        /// <summary>
        /// The heading for aerial imagery in degrees measured clockwise from cardinal direction North. 
        /// Headings are snapped to the nearest available angle for which imagery is available.
        /// </summary>
        public bool? KeyboardShortcuts { get; set; }

        /// <summary>
        /// The initial enabled/disabled state of the Map type control.
        /// </summary>
        public bool? MapTypeControl { get; set; }

        /// <summary>
        /// The maximum zoom level which will be displayed on the map. 
        /// If omitted, or set to null, the maximum zoom from the current map type is used instead. 
        /// Valid values: Integers between zero, and up to the supported maximum zoom level.
        /// </summary>
        public int? MaxZoom { get; set; }

        /// <summary>
        /// The minimum zoom level which will be displayed on the map. 
        /// If omitted, or set to null, the minimum zoom from the current map type is used instead. 
        /// Valid values: Integers between zero, and up to the supported maximum zoom level.
        /// </summary>
        public int? MinZoom { get; set; }

        /// <summary>
        /// If true, do not clear the contents of the Map div.
        /// </summary>
        public bool? NoClear { get; set; }

        /// <summary>
        /// The enabled/disabled state of the Pan control.
        /// </summary>
        public bool? PanControl { get; set; }

        /// <summary>
        /// Controls the automatic switching behavior for the angle of incidence of the map. 
        /// The only allowed values are 0 and 45. The value 0 causes the map to always use a 0° overhead view regardless of the zoom level and viewport. 
        /// The value 45 causes the tilt angle to automatically switch to 45 whenever 45° imagery is available for the current zoom level and viewport, and switch back to 0 whenever 45° imagery is not available (this is the default behavior). 45° imagery is only available for satellite and hybrid map types, within some locations, and at some zoom levels. 
        /// Note: getTilt returns the current tilt angle, not the value specified by this option. 
        /// </summary>
        public int? Tilt { get; set; }

        /// <summary>
        /// The initial Map zoom level. 
        /// Required. 
        /// Valid values: Integers between zero, and up to the supported maximum zoom level.
        /// </summary>
        public int? Zoom { get; set; }

    }
}
