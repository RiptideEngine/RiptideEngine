using System.Runtime.CompilerServices;

namespace Riptide.LowLevel.TextEngine.RectPack; 

public unsafe struct stbrp_context {
    public int Width;
    public int Height;
    public int Align;
    public int InitMode;
    public int Heuristic;
    public int NumNodes;
    public stbrp_node* ActiveHead;
    public stbrp_node* FreeHead;
    public ExtraNodes Extra;
    
    [InlineArray(2)]
    public struct ExtraNodes {
        private stbrp_node _element0;
    }
}