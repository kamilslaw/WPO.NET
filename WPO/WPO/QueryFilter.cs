namespace WPO
{
    public class QueryFilter
    {
        public WPOTableObject Table { get; set; }
        
        public int? Skip { get; set; }

        public int? Take { get; set; }

        public Statement Conditions { get; set; }

        public Order Orders { get; set; }

        public QueryFilter(WPOTableObject table)
        {
            Table = table;
            Clear();
        }

        public void Clear()
        {
            Conditions = null;
            Orders = null;
            Skip = null;
            Take = null;
        }

        public void AddCondition(string value, Statement.Operator @operator = Statement.Operator.UNDEFINED)
        {
            Statement newCondition = new Statement() { Value = value };
            if (Conditions == null)
            {
                Conditions = newCondition;
            }
            else
            {
                Statement tmpStatement = Conditions;
                while (tmpStatement.NextStatement != null)
                {
                    tmpStatement = tmpStatement.NextStatement;
                }

                tmpStatement.LogicalOperator = @operator;
                tmpStatement.NextStatement = newCondition;
            }
        }

        public void AddOrder(string column, Order.Direction direction)
        {
            Order newOrder = new Order() { ColumnName = column, OrderingType = direction };
            if (Orders == null)
            {
                Orders = newOrder;
            }
            else
            {
                Order tmpOrder = Orders;
                while (tmpOrder.NextOrder != null)
                {
                    tmpOrder = tmpOrder.NextOrder;
                }

                tmpOrder.NextOrder = newOrder;
            }
        }

        public class Statement
        {
            public string Value { get; set; }

            public Operator LogicalOperator { get; set; }

            public Statement NextStatement { get; set; }

            public enum Operator
            {
                UNDEFINED,
                AND,
                OR
            }
        }

        public class Order
        {
            public string ColumnName { get; set; }

            public Order NextOrder { get; set; }

            public Direction OrderingType { get; set; }

            public enum Direction
            {
                ASC,
                DESC
            }
        }
    }
}
