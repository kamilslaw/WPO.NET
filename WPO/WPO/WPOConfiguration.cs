using System;

namespace WPO
{
    public class WPOConfiguration
    {
        public static WPOConfiguration DefaultConfiguration = new WPOConfiguration() { DependencyDepth = 2 };

        private int dependencyDepth;
        /// <summary>
        /// Determine how many succeeding relations are automatically load (-1 for load ALL relations) 
        /// </summary>
        public int DependencyDepth
        {
            get { return dependencyDepth; }
            set
            {
                if (value > 0 || value == -1)
                {
                    dependencyDepth = value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
