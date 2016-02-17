using System.Collections.Generic;

namespace BSUG.Newsletter.Utility.Entities
{
    public class ContentCategory : Category
    {
        public List<ContentItem> Articles { get; set; }

        public ContentCategory()
        {
            Articles = new List<ContentItem>();
        }
    }
}
