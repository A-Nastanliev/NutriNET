using System;
using System.Collections.Generic;
using System.Text;

namespace NutriNET.Maui.ApiClients
{
    public static class PaginationQuery
    {
        public static string Build(int count, DateTime? cursorDate, int? cursorId)
        {
            var qs = $"?count={count}";

            if (cursorDate.HasValue)
                qs += $"&cursorDate={cursorDate:O}";

            if (cursorId.HasValue)
                qs += $"&cursorId={cursorId}";

            return qs;
        }
    }
}
