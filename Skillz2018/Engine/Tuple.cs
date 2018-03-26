namespace MyBot.Engine
{
    public class Tuple<T>
    {
        public T arg0;
        public Tuple(T arg0)
        {
            this.arg0 = arg0;
        }

        public override string ToString()
        {
            return "<" + arg0.ToString() + ">";
        }
    }
    public class Tuple<T, U> : Tuple<T>
    {
        public U arg1;
        public Tuple(T arg0, U arg1) : base(arg0)
        {
            this.arg1 = arg1;
        }

        public override string ToString()
        {
            return "<" + arg0.ToString() +", " + arg1.ToString() + ">";
        }
    }
    public class Tuple<T, U, W> : Tuple<T, U>
    {
        public W arg2;
        public Tuple(T arg0, U arg1, W arg2) : base(arg0, arg1)
        {
            this.arg2 = arg2;
        }

        public override string ToString()
        {
            return "<" + arg0.ToString() + ", " + arg1.ToString() +", " + arg2.ToString() + ">";
        }
    }
}
