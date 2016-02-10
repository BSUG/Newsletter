using System.Collections.Generic;

namespace BSUG.Newsletter.Utility.Entities
{
    public class PeopleCategory : Category
    {
        public List<PeopleItem> Links { get; set; }

        public PeopleCategory()
        {
            Links = new List<PeopleItem>();
        }
    }
}
