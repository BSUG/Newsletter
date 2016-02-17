namespace BSUG.Newsletter.Utility.Entities
{
    public class Episode
    {
        public string DigestTitle { get; set; }
        public string TitleImg { get; set; }

        public int Number { get; set; }

        public ContentCategory News { get; set; }
        public ContentCategory Tools { get; set; }
        public ContentCategory Novices { get; set; }
        public ContentCategory Videos { get; set; }
        public ContentCategory Blogs { get; set; }
        public ContentCategory Events { get; set; }

        public PeopleCategory People { get; set; }

        public Category Humor { get; set; }

        public Episode()
        {
            News = new ContentCategory();
            Tools = new ContentCategory();
            Novices = new ContentCategory();
            Videos = new ContentCategory();
            Blogs = new ContentCategory();
            Events = new ContentCategory();

            People = new PeopleCategory();

            Humor = new ContentCategory();
        }
    }
}
