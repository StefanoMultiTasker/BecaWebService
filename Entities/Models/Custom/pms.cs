using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Models.Custom
{
    public class pmsJson
    {
        public int external_user_id { get; set; }
        public int user_process_id { get; set; }
        public string? user_process_status { get; set; }
        public object? user_process_content { get; set; }
        public List<pmsJsonStep> user_steps { get; set; }
    }

    public class pmsJsonStep
    {
        public int user_step_id { get; set; }
        public string status { get; set; }
        public object? content { get; set; }
    }

    public class pmsPost
    {
        public int template_process_id { get; set; }
        public List<pmsPostData> data { get; set; }
    }

    public class pmsPostData
    {
        public int external_user_id { get; set; }
        public object content { get; set; }
        public string email { get; set; }
        public string apl { get; set; }
        public List<string> communication { get; set; }
    }

    public class pmsPostResponse
    {
        public List<pmsPostResponseData> response { get; set; }
    }
    public class pmsPostResponseData
    {
        public int external_user_id { get; set; }
        public int user_process_id { get; set; }

        public List<int> user_step_ids { get; set; }
        public string link { get; set; }
    }
}
