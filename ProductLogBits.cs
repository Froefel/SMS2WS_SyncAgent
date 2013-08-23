using System;

namespace SMS2WS_SyncAgent
{
    public class ProductLogBits
    {
        public string BitData { get; private set; }

        public ProductLogBits(string bitData)
        {
            BitData = bitData.PadRight(100, '0');
        }

        public ProductLogBits()
        {
            BitData = new string('0', 100);
        }

        /// <summary>
        /// Sets a specific bit to a specific value.
        /// </summary>
        /// <param name="position">Position of the bit to be set</param>
        /// <param name="value">Value of the bit</param>
        public void BitSet(Enums.Logfield position, bool value)
        {
            char[] BitDataArray = BitData.ToCharArray();
            BitDataArray[(int)position - 1] = value == true ? '1' : '0';
            BitData = new string(BitDataArray);
        }

        
        /// <summary>
        /// Inverses the value of a specific bit from "0" to "1" or from "1" to "0"
        /// </summary>
        /// <param name="position">Position of the bit to be set</param>
        public void BitFlip(Enums.Logfield position)
        {
            bool value = Convert.ToBoolean(BitData[(int)position - 1]);
            value = !value;
            BitSet(position, value);
        }

        
        /// <summary>
        /// Gets the value of a specific bit
        /// </summary>
        /// <param name="position">Position of the bit to be set</param>
        /// <returns></returns>
        public bool BitTest(Enums.Logfield position)
        {
            bool result = (BitData[(int) position - 1]) != '0';
            return result;
        }


        /// <summary>
        /// Generates a human-readable version of the current BitData.
        /// By default, fields with a bit value of "0" are included in the output. Setting IncludeOffBits to
        /// False will omit these fields from the output.
        /// </summary>
        /// <param name="includeOffBits">Indicating whether bits with a value of "0" are included in the output</param>
        /// <returns></returns>
        public string ToString(bool includeOffBits)
        {
            string result = "";
            int maxFields = (int) Enums.GetHighestValue<Enums.Logfield>();
            
            for (int i = 1; i <= maxFields; i++)
            {
                var iAsLogField = (Enums.Logfield)i;
                
                if (Enum.IsDefined(typeof(Enums.Logfield), iAsLogField))
                {
                    if (includeOffBits || BitTest(iAsLogField))
                    {
                        result += String.Format("{0} - {1}{2}\n",
                                                i.ToString().PadLeft(3, '0'),
                                                iAsLogField.ToString(),
                                                includeOffBits == true ? " = " + BitTest(iAsLogField) : null
                                               );
                    }
                }
            }

            return result;
        }

        
        /// <summary>
        /// Generates a human-readable version of the current BitData, including all bits with a value of "0".
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ToString(true);
        }
    }
}
