using System;
using thinger.ConfigLib;

namespace CX102PrickHMI.Utilities
{
    /// <summary>
    /// PLC 标签地址查找辅助类：按变量名在 OPCUA 设备的所有分组中查找变量地址。
    /// </summary>
    public static class PlcTagLookup
    {
        /// <summary>
        /// 遍历设备的所有 GroupList.VarList，按变量名（忽略大小写）查找对应的变量地址。
        /// </summary>
        /// <param name="device">OPCUA 设备。</param>
        /// <param name="varName">要查找的变量名。</param>
        /// <param name="address">找到时为变量地址；未找到或入参无效时为 null。</param>
        /// <returns>找到返回 true，否则 false。</returns>
        public static bool TryGetAddress(OPCUADevice device, string varName, out string address)
        {
            if (device == null || string.IsNullOrWhiteSpace(varName))
            {
                address = null;
                return false;
            }

            if (device.GroupList != null)
            {
                foreach (var group in device.GroupList)
                {
                    if (group == null || group.VarList == null)
                    {
                        continue;
                    }

                    foreach (var variable in group.VarList)
                    {
                        if (variable == null)
                        {
                            continue;
                        }

                        if (string.Equals(variable.VarName, varName, StringComparison.OrdinalIgnoreCase))
                        {
                            address = variable.VarAddress;
                            return true;
                        }
                    }
                }
            }

            address = null;
            return false;
        }
    }
}
