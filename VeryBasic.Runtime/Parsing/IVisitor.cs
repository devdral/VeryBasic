namespace VeryBasic.Runtime.Parsing;

public interface IVisitor<out T>
{
    public T VisitValueNode(ValueNode node);
    public T VisitUnaryOpNode(UnaryOpNode node);
    public T VisitBinaryOpNode(BinaryOpNode node);
    public T VisitTheResultNode(TheResultNode node);
    public T VisitVarDecNode(VarDecNode node);
    public T VisitVarSetNode(VarSetNode node);
    public T VisitProcCallNode(ProcCallNode node);
    public T VisitVarRefNode(VarRefNode node);
    public T VisitIfNode(IfNode node);
    public T VisitWhileLoopNode(WhileLoopNode node);
    public T VisitRepeatLoopNode(RepeatLoopNode node);
    public T VisitListNode(ListNode node);
    public T VisitListGetNode(ListGetNode node);
    public T VisitListSetNode(ListSetNode node);
    public T VisitProcDefNode(ProcDefNode node);
    public T VisitConvertNode(ConvertNode node);
    public T VisitReturnNode(ReturnNode node);
}