namespace OverScript
{
    public class Expr
    {
        public EvalUnit EU;
        public EvalUnit OrigEU;
        public Expr(EvalUnit eu, EvalUnit orig = null)
        {
            EU = eu;
            OrigEU = orig;
        }
        public override string ToString() => EU.ToString();
    }
}
