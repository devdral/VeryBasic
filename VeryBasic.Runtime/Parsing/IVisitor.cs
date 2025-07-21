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
}