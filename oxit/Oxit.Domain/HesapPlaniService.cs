﻿using AutoMapper;
using Oxit.Domain.Models;
using Microsoft.Extensions.Configuration;
using Oxit.DataAccess.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Oxit.DataAccess.teknopark;

namespace Oxit.Domain
{
    public class HesapPlaniService
    {
        private readonly appDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public HesapPlaniService(IMapper mapper,
            IConfiguration configuration,
            appDbContext appDbContext)
        {
            _configuration = configuration;
            _dbContext = appDbContext;
        }

        public void GecikmeleriHesapla(string? hesapkodu)
        {
            var gecikmeOrani = Convert.ToDecimal(_configuration.GetSection("gecikmeOrani").Value);
            var gecikmeGunu = Convert.ToInt32(_configuration.GetSection("gecikmeGunu").Value);
            var gecikmeBaslamaTarihi = Convert.ToDateTime(_configuration.GetSection("gecikmeBaslamaTarihi").Value);

            List<Fis>? fisBorc;
            List<Fis>? fisGecikme;
            var isHesapAktifmi = _dbContext.HesapPlani.Where(x => x.Aktif && x.Kod == hesapkodu);

            if (isHesapAktifmi.Count() > 0)
            {
                fisBorc = _dbContext.Fis
                    .Include(y => y.HesapPlani)
                    .Where(y => y.HesapPlani.Kod == hesapkodu && y.Borc > 0
                                                              && y.Tarih >= gecikmeBaslamaTarihi && !y.Odendi &&
                                                              ( y.FisTur == "3" || y.FisTur == "6"))
                    .OrderBy(y => y.Tarih).ThenBy(n => n.Id)
                    .ToList();

                foreach (var item in fisBorc)
                {
                    var AlacakT = _dbContext.Fis
                        .Include(y => y.HesapPlani)
                        .Where(y => y.HesapPlani.Kod == hesapkodu && y.Tarih >= item.Tarih && y.Alacak > 0 &&
                                    !y.Odendi && y.FisTur == "3"
                                    && y.Tarih >= gecikmeBaslamaTarihi)
                        .OrderBy(y => y.Tarih).FirstOrDefault();

                    if (((
                            AlacakT == null ? DateTime.Now : (DateTime) AlacakT.Tarih) - (DateTime) item.Tarih)
                        .TotalDays > gecikmeGunu)
                    {
                        var borcTutari = item.KalanBorcTutar > 0 ? (double) item.KalanBorcTutar : (double) item.Borc;
                        int gecikmeGun = Convert.ToInt32((DateTime.Now - (DateTime)item.Tarih).TotalDays) -1;
                        
                        if (borcTutari > 0)
                        {
                            if (gecikmeGun >= gecikmeGunu)
                            {
                                if (item.FisTur != "6")
                                    if (_dbContext.Fis.Where(y => y.FisBorcId == item.Id && y.FisTur=="5" && !y.Odendi && y.HesapPlaniId == item.HesapPlaniId).Any())
                                    {
                                        _dbContext.Fis.Remove(_dbContext.Fis.Where(y => y.FisBorcId == item.Id && !y.Odendi && y.FisTur=="5" && 
                                                                                                y.HesapPlaniId == item.HesapPlaniId).FirstOrDefault());
                                        _dbContext.SaveChanges();

                                        Fis fis = new()
                                        {
                                            HesapPlaniId = item.HesapPlaniId,
                                            Tarih = item.Tarih,
                                            FisBorcId = item.Id,
                                            GecikmeTutari =
                                                Math.Round(
                                                    (((borcTutari * (double)gecikmeOrani) * (int)gecikmeGun) / 36000)  , 2) *
                                                1.18,
                                            GecikmeGunu = (int) gecikmeGun,
                                            Odendi = false,
                                            FisTur = "5",
                                          
                                            SonHesaplananGecikmeTarihi = DateTime.Now
                                        };
                                        _dbContext.Fis.Add(fis);
                                        _dbContext.SaveChanges();
                                    }
                                    else
                                    {
                                        Fis fis = new()
                                        {
                                            HesapPlaniId = item.HesapPlaniId,
                                            Tarih = item.Tarih,
                                            FisBorcId = item.Id,
                                            GecikmeTutari =
                                                Math.Round(
                                                    (((borcTutari * (double)gecikmeOrani) * (int)gecikmeGun) / 36000) , 2) *
                                                1.18,
                                            GecikmeGunu = (int) gecikmeGun,
                                            Odendi = false,
                                            FisTur = "5",
                                          
                                            SonHesaplananGecikmeTarihi = DateTime.Now
                                        };
                                        _dbContext.Fis.Add(fis);
                                        _dbContext.SaveChanges();
                                    }
                            }
                        }
                    }
                }

                var fisAlacakList = _dbContext.Fis
                    .Include(y => y.HesapPlani)
                    .Where(y => y.HesapPlani.Kod == hesapkodu && y.Alacak > 0
                                                              && y.Tarih >= gecikmeBaslamaTarihi && !y.Odendi
                    )
                    .OrderBy(y => y.Tarih).ThenBy(n => n.Id)
                    .ToList();


                foreach (var itemsAlacaks in fisAlacakList)
                {
                    double? itemAlacakTutari = itemsAlacaks.Alacak;
                    
                   if  (itemsAlacaks.Tarih < new DateTime(2022, 7, 31))
                   {
                     #region BorcBakiye hesapla
                   
                    var fisBorcBakiyes = _dbContext.Fis
                        .Include(y => y.HesapPlani)
                        .Where(y => y.HesapPlani.Kod == hesapkodu && y.Borc > 0 &&
                                    y.FisTur == "6" && y.Tarih >= gecikmeBaslamaTarihi &&
                                    !y.Odendi)
                        .FirstOrDefault();

                    if (fisBorcBakiyes is not null )
                    {
                        if (itemAlacakTutari > (fisBorcBakiyes.Borc - (fisBorcBakiyes.OdenenBorcTutar ?? 0)))
                        {
                            itemAlacakTutari = itemAlacakTutari - (fisBorcBakiyes.Borc - (fisBorcBakiyes.OdenenBorcTutar ?? 0));
                            fisBorcBakiyes.OdenenBorcTutar = fisBorcBakiyes.Borc;
                            itemsAlacaks.KalanAlacakTutar =
                                itemAlacakTutari > (fisBorcBakiyes.Borc - (fisBorcBakiyes.OdenenBorcTutar ?? 0))
                                    ? itemAlacakTutari - (fisBorcBakiyes.Borc - (fisBorcBakiyes.OdenenBorcTutar ?? 0))
                                    : 0;
                            fisBorcBakiyes.KalanBorcTutar = 0;
                            fisBorcBakiyes.GecikmeFisId = itemsAlacaks.Id;
                            fisBorcBakiyes.Odendi = true;
                        }
                        else
                        {
                            var odenen = fisBorcBakiyes.OdenenBorcTutar != null ? (double)(fisBorcBakiyes.OdenenBorcTutar + itemAlacakTutari) : itemAlacakTutari;
                            fisBorcBakiyes.OdenenBorcTutar = odenen;
                            itemAlacakTutari = 0;
                            itemsAlacaks.KalanAlacakTutar = 0;
                            itemsAlacaks.Odendi = true;
                            fisBorcBakiyes.Odendi = fisBorcBakiyes.Borc == fisBorcBakiyes.OdenenBorcTutar ? true : false;
                            fisBorcBakiyes.KalanBorcTutar = fisBorcBakiyes.Borc - odenen;
                            fisBorcBakiyes.GecikmeFisId = itemsAlacaks.Id;
                        }

                        itemsAlacaks.KalanBorcTutar = 0;
                        _dbContext.SaveChanges(); 
                    }   
                    #endregion
                   }
                   else
                    {
                        #region Bakiye Gecikmeleri hesapla 
                            var fisGecikmeBakiye = _dbContext.Fis
                                .Include(y => y.HesapPlani)
                                .Where(y => y.HesapPlani.Kod == hesapkodu && y.FisTur == "7"
                                                                          && y.GecikmeTutari != y.OdenenGecikmeTutar
                                )
                              
                                .FirstOrDefault();
                            if (fisGecikmeBakiye is not null)
                            {
                                if (itemAlacakTutari > fisGecikmeBakiye.GecikmeTutari)
                                {
                                    itemAlacakTutari = itemAlacakTutari - fisGecikmeBakiye.GecikmeTutari;
                                    fisGecikmeBakiye.OdenenGecikmeTutar = fisGecikmeBakiye.GecikmeTutari;
                                    fisGecikmeBakiye.GecikmeFisId = itemsAlacaks.Id;
                                    fisGecikmeBakiye.Odendi = true;
                                }
                                else
                                {
                                    fisGecikmeBakiye.OdenenGecikmeTutar = fisGecikmeBakiye.GecikmeTutari - itemAlacakTutari;
                                    itemsAlacaks.KalanGecikmeTutar = itemsAlacaks.Alacak;
                                    fisGecikmeBakiye.KalanAlacakTutar = 0;
                                    fisGecikmeBakiye.GecikmeFisId = itemsAlacaks.Id;
                                    itemAlacakTutari = 0;
                                }

                                itemsAlacaks.KalanAlacakTutar = itemAlacakTutari;
                                _dbContext.SaveChanges();
                            }
                            #endregion
                            
                        #region BorcBakiye hesapla
                   
                    var fisBorcBakiyes = _dbContext.Fis
                        .Include(y => y.HesapPlani)
                        .Where(y => y.HesapPlani.Kod == hesapkodu && y.Borc > 0 &&
                                    y.FisTur == "6" && y.Tarih >= gecikmeBaslamaTarihi &&
                                    !y.Odendi)
                        .OrderBy(y => y.Tarih).ThenBy(n => n.Id)
                        .FirstOrDefault();

                    if (fisBorcBakiyes is not null )
                    {
                        if (itemAlacakTutari > (fisBorcBakiyes.Borc - (fisBorcBakiyes.OdenenBorcTutar ?? 0)))
                        {
                            itemAlacakTutari = itemAlacakTutari - (fisBorcBakiyes.Borc - (fisBorcBakiyes.OdenenBorcTutar ?? 0));
                            fisBorcBakiyes.OdenenBorcTutar = fisBorcBakiyes.Borc;
                            itemsAlacaks.KalanAlacakTutar =
                                itemAlacakTutari > (fisBorcBakiyes.Borc - (fisBorcBakiyes.OdenenBorcTutar ?? 0))
                                    ? itemAlacakTutari - (fisBorcBakiyes.Borc - (fisBorcBakiyes.OdenenBorcTutar ?? 0))
                                    : 0;
                            fisBorcBakiyes.KalanBorcTutar = 0;
                            fisBorcBakiyes.GecikmeFisId = itemsAlacaks.Id;
                            fisBorcBakiyes.Odendi = true;
                        }
                        else
                        {
                            var odenen = fisBorcBakiyes.OdenenBorcTutar != null ? (double)(fisBorcBakiyes.OdenenBorcTutar + itemAlacakTutari) : itemAlacakTutari;
                            fisBorcBakiyes.OdenenBorcTutar = odenen;
                            itemAlacakTutari = 0;
                            itemsAlacaks.KalanAlacakTutar = 0;
                            itemsAlacaks.Odendi = true;
                            fisBorcBakiyes.Odendi = fisBorcBakiyes.Borc == fisBorcBakiyes.OdenenBorcTutar ? true : false;
                            fisBorcBakiyes.KalanBorcTutar = fisBorcBakiyes.Borc - odenen;
                            fisBorcBakiyes.GecikmeFisId = itemsAlacaks.Id;
                        }

                        itemsAlacaks.KalanBorcTutar = 0;
                        _dbContext.SaveChanges(); 
                    }
                    #endregion
                    }
                   
                   #region Gecikmeleri hesapla
                   if (itemAlacakTutari>0)
                   {
                       var fisGecikmes = _dbContext.Fis
                           .Include(y => y.HesapPlani)
                           .Where(y => y.HesapPlani.Kod == hesapkodu && y.Tarih >= gecikmeBaslamaTarihi &&
                       //                y.Tarih >= itemsAlacaks.Tarih 
                       y.FisTur == "5" 
                                                                     && y.GecikmeTutari != y.OdenenGecikmeTutar) 

                           .OrderBy(y => y.Id)
                           //  .ThenBy(n => n.Id)
                           .ToList();

                       foreach (var itemGecikme in fisGecikmes)
                       {
                           if (itemAlacakTutari > itemGecikme.GecikmeTutari)
                           {
                               itemAlacakTutari = itemAlacakTutari - itemGecikme.GecikmeTutari;
                               itemGecikme.OdenenGecikmeTutar = itemGecikme.GecikmeTutari;
                               itemGecikme.GecikmeFisId = itemsAlacaks.Id;
                               itemGecikme.Odendi = true;
                           }
                           else
                           {
                               itemGecikme.OdenenGecikmeTutar = itemGecikme.GecikmeTutari - itemAlacakTutari;
                               itemsAlacaks.KalanGecikmeTutar = itemsAlacaks.Alacak;
                               itemGecikme.GecikmeFisId = itemsAlacaks.Id;
                               itemAlacakTutari = 0;
                           }

                           itemsAlacaks.KalanAlacakTutar = itemAlacakTutari;
                           _dbContext.SaveChanges();
                       }
                   }
                   #endregion

                    
                    #region 45 gun altı Borcları hesapla
                    var fisBorcs = _dbContext.Fis
                        .Include(y => y.HesapPlani)
                        .Where(y => y.HesapPlani.Kod == hesapkodu && y.Borc > 0 &&
                                    y.FisTur == "3"  && y.Tarih >= gecikmeBaslamaTarihi &&
                                    !y.Odendi)
                        .OrderBy(y => y.Id)
                      //  .ThenBy(n => n.Id)
                        .ToList();

                    foreach (var itemBorcs in fisBorcs)
                    {
                        // if (  (((Convert.ToInt32(((DateTime)itemsAlacaks.Tarih - (DateTime)itemBorcs.Tarih).TotalDays)) -1) <= 45 ))
                        //if (  (((Convert.ToInt32((new DateTime(2022,6,30) ).TotalDays)) -1) <= 30 ))
                        {
                        if (itemAlacakTutari > (itemBorcs.Borc - (itemBorcs.OdenenBorcTutar ?? 0)))
                        {
                            itemAlacakTutari = itemAlacakTutari - (itemBorcs.Borc - (itemBorcs.OdenenBorcTutar ?? 0));
                            itemBorcs.OdenenBorcTutar = itemBorcs.Borc;
                            itemsAlacaks.KalanAlacakTutar =
                                itemAlacakTutari > (itemBorcs.Borc - (itemBorcs.OdenenBorcTutar ?? 0))
                                    ? itemAlacakTutari - (itemBorcs.Borc - (itemBorcs.OdenenBorcTutar ?? 0))
                                    : 0;
                            itemBorcs.KalanBorcTutar = 0;
                            itemBorcs.GecikmeFisId = itemsAlacaks.Id;

                            itemBorcs.Odendi = true;
                        }
                        else
                        {
                            itemBorcs.OdenenBorcTutar = itemAlacakTutari;
                            itemBorcs.KalanBorcTutar = itemBorcs.Borc - itemAlacakTutari;
                            itemAlacakTutari = 0;
                            itemsAlacaks.KalanAlacakTutar = 0;
                            itemsAlacaks.Odendi = true;
                            itemBorcs.Odendi = itemBorcs.Borc == itemBorcs.OdenenBorcTutar ? true : false;
                            itemBorcs.KalanBorcTutar = itemBorcs.KalanBorcTutar == itemBorcs.OdenenBorcTutar
                                ? 0
                                : itemBorcs.KalanBorcTutar;
                            itemBorcs.GecikmeFisId = itemsAlacaks.Id;
                        }
                        itemsAlacaks.KalanBorcTutar = 0;
                        _dbContext.SaveChanges();
                        }
                     }
                    #endregion
                    
                   
                    
                    //  #region 45 gun üstü Borcları hesapla
                    //  if (itemAlacakTutari>0)
                    //  {
                    //  var fisBorcs45 = _dbContext.Fis
                    //     .Include(y => y.HesapPlani)
                    //     .Where(y => y.HesapPlani.Kod == hesapkodu && y.Borc > 0 &&
                    //              
                    //                 y.FisTur == "3"  && y.Tarih >= gecikmeBaslamaTarihi &&
                    //                 !y.Odendi)
                    //     //.OrderBy(y => y.Tarih).ThenBy(n => n.Id)
                    //     .OrderBy(y => y.Id)
                    //     //  .ThenBy(n => n.Id)
                    //     .ToList();
                    //
                    // foreach (var itemBorcs in fisBorcs45)
                    // {
                    //     if ((((Convert.ToInt32(((DateTime)itemsAlacaks.Tarih - (DateTime)itemBorcs.Tarih).TotalDays)) -1) > 45 ))
                    //     {
                    //     if (itemAlacakTutari > (itemBorcs.Borc - (itemBorcs.OdenenBorcTutar ?? 0)))
                    //     {
                    //         itemAlacakTutari = itemAlacakTutari - (itemBorcs.Borc - (itemBorcs.OdenenBorcTutar ?? 0));
                    //         itemBorcs.OdenenBorcTutar = itemBorcs.Borc;
                    //         itemsAlacaks.KalanAlacakTutar =
                    //             itemAlacakTutari > (itemBorcs.Borc - (itemBorcs.OdenenBorcTutar ?? 0))
                    //                 ? itemAlacakTutari - (itemBorcs.Borc - (itemBorcs.OdenenBorcTutar ?? 0))
                    //                 : 0;
                    //         itemBorcs.KalanBorcTutar = 0;
                    //         itemBorcs.GecikmeFisId = itemsAlacaks.Id;
                    //
                    //         itemBorcs.Odendi = true;
                    //     }
                    //     else
                    //     {
                    //         itemBorcs.OdenenBorcTutar = itemAlacakTutari;
                    //         itemBorcs.KalanBorcTutar = itemBorcs.Borc - itemAlacakTutari;
                    //         itemAlacakTutari = 0;
                    //         itemsAlacaks.KalanAlacakTutar = 0;
                    //         itemsAlacaks.Odendi = true;
                    //         itemBorcs.Odendi = itemBorcs.Borc == itemBorcs.OdenenBorcTutar ? true : false;
                    //         itemBorcs.KalanBorcTutar = itemBorcs.KalanBorcTutar == itemBorcs.OdenenBorcTutar
                    //             ? 0
                    //             : itemBorcs.KalanBorcTutar;
                    //         itemBorcs.GecikmeFisId = itemsAlacaks.Id;
                    //     }
                    //     itemsAlacaks.KalanBorcTutar = 0;
                    //     _dbContext.SaveChanges();
                    //  }
                    // }
                    //  }
                    // #endregion
                }
            }
       
        }

        public void GecikmeleriHesaplaBakiye(string? hesapkodu)
        {
            var gecikmeOrani = Convert.ToDecimal(_configuration.GetSection("gecikmeOrani").Value);
            var gecikmeGunu = 30;
            var gecikmeBaslamaTarihi = Convert.ToDateTime(_configuration.GetSection("gecikmeBaslamaTarihi").Value);

            Fis? fisBorc;
            var isHesapAktifmi = _dbContext.HesapPlani.Where(x => x.Aktif && x.Kod == hesapkodu);

            if (isHesapAktifmi.Count() > 0)
            {
                fisBorc = _dbContext.Fis
                    .Include(y => y.HesapPlani)
                    .Where(y => y.HesapPlani.Kod == hesapkodu && !y.Odendi && y.FisTur == "6")
                    .OrderBy(y => y.Tarih).ThenBy(n => n.Id)
                    .FirstOrDefault();

                if (fisBorc is not null)
                {
                    var alacakTutari = _dbContext.Fis
                        .Include(y => y.HesapPlani)
                        .Where(y => y.HesapPlani.Kod == hesapkodu && y.Alacak > 0 &&
                                    y.Tarih >= gecikmeBaslamaTarihi && !y.Odendi &&
                                    y.Tarih <= gecikmeBaslamaTarihi.AddDays(30)
                        )
                        .Sum(z => z.Alacak);

                    var firstAlacakTutarTarihi = _dbContext.Fis
                        .Include(y => y.HesapPlani)
                        .Where(y => y.HesapPlani.Kod == hesapkodu && y.Alacak > 0 &&
                                    y.Tarih >= gecikmeBaslamaTarihi && !y.Odendi &&
                                    y.Tarih <= DateTime.Now
                        )
                        .FirstOrDefault();
                    
   
                    var borcTutari = (double) (fisBorc.KalanBorcTutar > 0
                        ? fisBorc.KalanBorcTutar
                        : fisBorc.Borc - alacakTutari);
                    
                    int gecikmeGun = 
                        // borcTutari <= 0 && firstAlacakTutarTarihi != null ? 
                        // Convert.ToInt32(((DateTime) firstAlacakTutarTarihi.Tarih - (DateTime) fisBorc.Tarih).TotalDays) -1 :
                        Convert.ToInt32((DateTime.Now - (DateTime) fisBorc.Tarih).TotalDays) -1;

                    if (gecikmeGun >= gecikmeGunu)
                    {
                        if ( 
                            Math.Round(
                                (((borcTutari * (double)gecikmeOrani) * (int)gecikmeGun) / 36000)  , 2) *
                            1.18 > 0)
                        {
                            
                           if  (!_dbContext.Fis.Where(y =>  y.FisTur=="7" && y.Odendi && y.HesapPlaniId == fisBorc.HesapPlaniId ).Any())
                           {
                            
                            
                            if (_dbContext.Fis.Where(y => y.FisBorcId == fisBorc.Id && y.FisTur=="7"  && y.HesapPlaniId == fisBorc.HesapPlaniId ).Any())
                            {
                                _dbContext.Fis.Remove(_dbContext.Fis.Where(y=> y.FisBorcId == fisBorc.Id && y.FisTur=="7"  && y.HesapPlaniId == fisBorc.HesapPlaniId)
                                    .FirstOrDefault());
                                _dbContext.SaveChanges();

                                Fis fis = new()
                                {
                                    HesapPlaniId = fisBorc.HesapPlaniId,
                                    Tarih = (DateTime) fisBorc.Tarih,
                                    FisBorcId = fisBorc.Id,
                                    GecikmeTutari =
                                        Math.Round(
                                            (((borcTutari * (double)gecikmeOrani) * (int)gecikmeGun) / 36000)  , 2) *
                                        1.18,
                                    GecikmeGunu = (int) gecikmeGun,
                                    Odendi = false,
                                    FisTur = "7",
                                    //GecikmeFisId = fisBorc.Id,
                                    SonHesaplananGecikmeTarihi = DateTime.Now
                                };
                                _dbContext.Fis.Add(fis);
                                _dbContext.SaveChanges();
                            }
                            else
                            {
                                Fis fis = new()
                                {
                                    HesapPlaniId = fisBorc.HesapPlaniId,
                                    Tarih = (DateTime) fisBorc.Tarih,
                                    FisBorcId = fisBorc.Id,
                                    GecikmeTutari =
                                        Math.Round(
                                            (((borcTutari * (double)gecikmeOrani) * (int)gecikmeGun) / 36000)  , 2) *
                                        1.18,
                                    GecikmeGunu = (int) gecikmeGun,
                                    Odendi = false,
                                    FisTur = "7",
                                    //GecikmeFisId = fisBorc.Id,
                                    SonHesaplananGecikmeTarihi = DateTime.Now
                                };
                                _dbContext.Fis.Add(fis);
                                _dbContext.SaveChanges();
                            }
                        }
                        }
                    }
                }
            }
        }

        public void AlacaksizGecikmeHesapla(string? hesapkodu)
        {
            var gecikmeOrani = Convert.ToDecimal(_configuration.GetSection("gecikmeOrani").Value);
            var gecikmeGunu = Convert.ToInt32(_configuration.GetSection("gecikmeGunu").Value);
            var gecikmeBaslamaTarihi = Convert.ToDateTime(_configuration.GetSection("gecikmeBaslamaTarihi").Value);

            List<Fis>? fisBorc;
            if (string.IsNullOrEmpty(hesapkodu))
            {
                fisBorc = _dbContext.Fis
                    .Include(y => y.HesapPlani)
                    .Where(y => y.Borc > 0 && !y.Odendi && y.FisTur == "3" && y.Tarih >= gecikmeBaslamaTarihi)
                    .OrderBy(y => y.Tarih).ThenBy(n => n.Id)
                    .ToList();
            }
            else
            {
                fisBorc = _dbContext.Fis
                    .Include(y => y.HesapPlani)
                    .Where(y => y.HesapPlani.Kod == hesapkodu && y.FisTur == "3"
                                                              && y.Tarih >= gecikmeBaslamaTarihi
                                                              && y.Borc > 0 && !y.Odendi)
                    .OrderBy(y => y.Tarih).ThenBy(n => n.Id)
                    .ToList();
            }

            foreach (var item in fisBorc)
            {
                var borcTutari = item.KalanTutar > 0 ? (double) item.KalanTutar : (double) item.Borc;
                var gecikmeGun = (DateTime.Now - (DateTime) item.Tarih).TotalDays;

                if (gecikmeGun >= gecikmeGunu)
                {
                    item.GeciktirilenAnaFaizTutar = (double) item.Borc;
                    item.GeciktirilenTutar = item.KalanTutar;
                    item.GecikmeGunu = (int) gecikmeGun;
                    item.GecikmeTutari =  
                        Math.Round(
                            (((borcTutari * (double)gecikmeOrani) * (int)gecikmeGun) / 36000)  , 2) *
                        1.18;
                    item.SonHesaplananGecikmeTarihi = DateTime.Now;
                }
                _dbContext.SaveChanges();
            }
        }
    }
}