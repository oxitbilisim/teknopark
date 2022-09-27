﻿namespace Oxit.Domain.ViewModels.Kira
{
    public class KiraReadDTO
    {
        public int Id { get; set; }
        public string? FirmaAdi { get; set; }
        public double? Metrekare { get; set; }
        public DateTime? BaslamaTarihi { get; set; }
        public DateTime? BitisTarihi { get; set; }


        public double? MetrekareKiraFiyati { get; set; }
        public double? MetrekareIsletmeFiyati { get; set; }

        public double? KiraKDVOrani { get; set; }
        public double? IsletmeKDVOrani { get; set; }
        public double? KiraBedeli { get; set; }
        public double? IsletmeBedeli { get; set; }
        public string? Aciklama { get; set; }

        public int KalanGun { get; set; }
    }



}