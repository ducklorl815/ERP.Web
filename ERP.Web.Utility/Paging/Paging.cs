using System;
using System.Collections.Generic;
using System.Text;

namespace LifeTech.ERP.Utility.Paging
{
    public class Paging
    {

        /// <summary></summary>
        /// <param name="pageNumber">頁碼</param>
        /// <param name="pageSize">每頁幾筆</param>
        /// <param name="totalCount">總筆數</param>
        /// <param name="showPageCount">顯示總頁數</param>
        public Paging(int pageNumber, int pageSize, int totalCount, int showPageCount = 5)
        {
            if (pageNumber <= 0)
                pageNumber = 1;
            if (pageSize < 1)
                throw new ArgumentOutOfRangeException("pageSize", "Value can not be less than 1.");

            PageSize = pageSize;
            PageIndex = pageNumber - 1;
            TotalItemCount = totalCount;
            PageCount = TotalItemCount > 0 ? (int)Math.Ceiling(TotalItemCount / (double)PageSize) : 0;

            HasPreviousPage = (PageIndex > 0);
            HasNextPage = (PageIndex < (PageCount - 1));
            IsFirstPage = (PageIndex <= 0);
            IsLastPage = (PageIndex >= (PageCount - 1));

            ItemStart = PageIndex * PageSize + 1;
            ItemEnd = Math.Min(PageIndex * PageSize + PageSize, TotalItemCount);

            if (TotalItemCount <= 0)
                return;

            PageStart = pageNumber - (int)Math.Floor(showPageCount / 2.0);
            if (PageStart < 1) { 
                PageStart = 1;
            }
            PageEnd = PageStart + showPageCount - 1;
            if (PageEnd >= PageCount)
            {
                PageEnd = PageCount;
            }
            if ((PageEnd - showPageCount + 1) != PageStart) PageStart = PageEnd - showPageCount + 1;
            if (PageStart <= 0) PageStart = 1;
        }
        /// <summary>總頁數</summary>
        public int PageCount { get; private set; }
        /// <summary>總筆數</summary>
        public int TotalItemCount { get; private set; }
        /// <summary>頁數索引(從0開始)</summary>
        public int PageIndex { get; private set; }
        /// <summary>頁數(從1開始)</summary>
        public int PageNumber { get { return PageIndex + 1; } }
        /// <summary>每頁幾筆</summary>
        public int PageSize { get; private set; }
        /// <summary>有上一頁</summary>
        public bool HasPreviousPage { get; private set; }
        /// <summary>有下一頁</summary>
        public bool HasNextPage { get; private set; }
        /// <summary>為第一頁</summary>
        public bool IsFirstPage { get; private set; }
        /// <summary>為最後一頁</summary>
        public bool IsLastPage { get; private set; }
        /// <summary>開始筆數</summary>
        public int ItemStart { get; private set; }
        /// <summary>結束筆數</summary>
        public int ItemEnd { get; private set; }
        /// <summary>開始頁數</summary>
        public int PageStart { get; private set; }
        /// <summary>結束頁數</summary>
        public int PageEnd { get; private set; }

    }


    public class PageViewModel
    {
        /// <summary>當前頁碼</summary>
        public int Page { get; set; } = 1;

        /// <summary>每頁幾筆</summary>
        public int PageSize { get; set; } = 15;

        /// <summary>分頁資訊</summary>
        public Paging Pager { get; set; }
    }

}
