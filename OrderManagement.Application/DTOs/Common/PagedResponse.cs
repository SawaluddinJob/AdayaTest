using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.DTOs.Common
{
    public class PagedResponse<T>
    {
        public List<T> Data { get; set; } = [];

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalRecords { get; set; }

        public int TotalPages =>
            (int)Math.Ceiling((double)TotalRecords / PageSize);
    }
}
