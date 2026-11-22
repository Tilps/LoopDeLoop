using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace LoopDeLoop.Web.Components
{
    public sealed class SvgClueText : ComponentBase
    {
        [Parameter, EditorRequired] public string X { get; set; } = string.Empty;
        [Parameter, EditorRequired] public string Y { get; set; } = string.Empty;
        [Parameter, EditorRequired] public string FontSize { get; set; } = string.Empty;
        [Parameter, EditorRequired] public string Color { get; set; } = string.Empty;
        [Parameter, EditorRequired] public int Clue { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "text");
            builder.AddAttribute(1, "x", X);
            builder.AddAttribute(2, "y", Y);
            builder.AddAttribute(3, "font-size", FontSize);
            builder.AddAttribute(4, "font-weight", "800");
            builder.AddAttribute(5, "font-family", "system-ui, -apple-system, sans-serif");
            builder.AddAttribute(6, "text-anchor", "middle");
            builder.AddAttribute(7, "dominant-baseline", "central");
            builder.AddAttribute(8, "fill", Color);
            builder.AddAttribute(9, "class", "cell-svg-text");
            builder.AddAttribute(10, "style", "pointer-events: none; user-select: none;");
            builder.AddContent(11, Clue);
            builder.CloseElement();
        }
    }
}
