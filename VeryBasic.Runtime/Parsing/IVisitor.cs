namespace VeryBasic.Runtime.Parsing;

public interface IVisitor<out T>
{
    public T VisitValueNode(ValueNode node);
    public T VisitUnaryOpNode(UnaryOpNode node);
    public T VisitBinaryOpNode(BinaryOpNode node);
    public T VisitTheResultNode(TheResultNode node);
}