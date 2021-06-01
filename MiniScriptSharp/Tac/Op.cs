namespace MiniScriptSharp.Tac {

    public enum Op {
        
        Noop = 0,
        AssignA,
        AssignImplicit,
        APlusB,
        AMinusB,
        ATimesB,
        ADividedByB,
        AModB,
        APowB,
        AEqualB,
        ANotEqualB,
        AGreaterThanB,
        AGreatOrEqualB,
        ALessThanB,
        ALessOrEqualB,
        AisaB,
        AAndB,
        AOrB,
        BindAssignA,
        CopyA,
        NotA,
        GotoA,
        GotoAifB,
        GotoAifTrulyB,
        GotoAifNotB,
        PushParam,
        CallFunctionA,
        CallIntrinsicA,
        ReturnA,
        ElemBofA,
        ElemBofIterA,
        LengthOfA
        
    }

}