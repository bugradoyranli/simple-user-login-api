
using System;
using System.ComponentModel.DataAnnotations;


    public class User
    {
        [Key]
        public int id { get; set; }
        [Required]
        [StringLength(11)]
        public string TcKimlikNo { get; set; }  
        public string Ad { get; set; } 
        public string Soyad { get; set; }

        [Required]
        public string Username { get; set; }

        public string HashPassword {  get; set;}

        [Required]
        public int BirthYear {get; set;}



        
        
    }

