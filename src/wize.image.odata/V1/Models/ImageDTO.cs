using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using wize.image.data.V1.Models;

namespace wize.image.odata.V1.Models
{
    public class ImageDTO// : Image
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid? ImageId { get; set; }
        public string Name { get; set; }
        public string OriginalName { get; set; }
        public string Url { get; set; }
        public string MIME { get; set; }
        [DefaultValue(true)]
        public bool Published { get; set; }
        private DateTime _created = DateTime.Now;
        [Required]
        public DateTime Created
        {
            get
            {
                return _created.ToLocalTime();
            }
            set
            {
                if (value.Kind == DateTimeKind.Utc)
                {
                    _created = value;
                }
                else if (value.Kind == DateTimeKind.Local)
                {
                    _created = value.ToUniversalTime();
                }
                else
                {
                    _created = DateTime.SpecifyKind(value, DateTimeKind.Utc);
                }
            }
        }
        public string CreatedBy { get; set; }
        public byte[] Blob { get; set; }
    }
}
