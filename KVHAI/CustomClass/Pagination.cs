using Microsoft.AspNetCore.Html;

namespace KVHAI.CustomClass
{
    public class Pagination<T>
    {
        public int NumberOfData { get; set; }
        public int Index { get; set; }
        public int ShowEntry { get; set; }
        public int CurrentPage { get; set; }
        public double MaxPage { get; set; }
        public int PageStart { get; set; }
        public int PageEnd { get; set; }
        public int NextPageSequence { get; set; }
        public int PrevPageSequence { get; set; }
        public int Offset { get; set; }
        public int MaximumPageEnd { get; set; }
        public int TotalRecord { get; set; }
        public string ScriptName { get; set; } = string.Empty;
        public HtmlString? Pagenation { get; set; }
        //public T? modelName { get; set; }
        public List<T>? ModelList { get; set; }


        public void set(int showEntries, int numberOfPagination, int currentPage)//List<T> modelList
        {
            //this.ModelList = modelList;
            this.ShowEntry = showEntries;
            this.CurrentPage = currentPage;         //500 / 10    = 50
            this.MaxPage = Math.Ceiling((double)NumberOfData / Convert.ToDouble(showEntries));//number of pagination 1,2,3...n
            //this.PageStart = (int)Math.Floor((double)(currentPage - 1) / showEntries) * showEntries + 1;//1
            this.PageStart = (int)Math.Floor((double)(currentPage - 1) / numberOfPagination) * numberOfPagination + 1;
            //this.end_page = start_page + 4;
            this.PageEnd = ((int)MaxPage > PageStart + (numberOfPagination - 1)) ? (PageStart + (numberOfPagination - 1)) : (int)MaxPage;
            //this.PageEnd = currentPage + 4;
            this.NextPageSequence = PageEnd + 1;
            //if prev < 0 prev disabled class
            this.PrevPageSequence = PageStart - numberOfPagination;
            this.Offset = (currentPage - 1) * showEntries;//start
            this.Index = Offset;


            this.MaximumPageEnd = currentPage * showEntries;//end
            if (MaximumPageEnd >= NumberOfData)
                this.MaximumPageEnd = NumberOfData;

            //this.total_no_page = (table.Rows.Count > 0) ? (((set_page - 1) * record_per_page) + 1) : 0;

            this.TotalRecord = NumberOfData;

            if (TotalRecord != 0)
                this.Pagenation = pageClass(currentPage);

        }

        public HtmlString pageClass(int page_set)
        {
            string onclickValue = "";

            string str1 = "<nav aria-label=\"Page navigation example\">";
            str1 += "<ul class=\"pagination\">";
            str1 += "<li class=\"page-item\"><a class=\"page-link " + (PrevPageSequence < 0 ? "disabled" : "") + "\" onclick=\"" + this.ScriptName + "(" + PrevPageSequence.ToString() + ")\">Previous</a></li>";

            //for anchor tag
            for (int page = PageStart, j = Offset; page <= PageEnd; page++, j++)
            {
                int pageNumber = page + 1;

                onclickValue = this.ScriptName + "(" + page.ToString() + ")";
                str1 += "<li class=\"page-item\"><a class=\"page-link " + (page == page_set ? "active" : "") + "  \" onclick=\"" + onclickValue + "\">" + page + "</a></li>";


            }
            str1 += "<li class=\"page-item\"><a class=\"page-link " + (PageEnd >= (int)MaxPage ? "disabled" : "") + "\" onclick=\"" + this.ScriptName + "(" + NextPageSequence.ToString() + ")\">Next</a></li>";
            str1 += "</ul></nav>";

            return new HtmlString(str1);
        }
    }
}
