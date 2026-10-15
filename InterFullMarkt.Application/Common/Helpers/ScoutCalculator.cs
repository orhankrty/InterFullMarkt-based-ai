namespace InterFullMarkt.Application.Common.Helpers;

using InterFullMarkt.Domain.Entities;
using InterFullMarkt.Domain.Enums;

/// <summary>
/// Futbolcu biyometrik, mevki ve piyasa verilerine dayalı Scouting ve Radar analitiği hesaplayıcısı.
/// FIFA/FM standartlarında 6 temel yetenek sütununu (Pace, Shooting, Passing, Dribbling, Defending, Physicality)
/// ve Wonderkid potansiyelini dinamik hesaplar.
/// </summary>
public static class ScoutCalculator
{
    public sealed record PlayerScoutMetrics(
        int Pace,
        int Shooting,
        int Passing,
        int Dribbling,
        int Defending,
        int Physicality,
        int ScoutRating,
        bool IsWonderkid,
        string ScoutTier,
        decimal PotentialGrowthPercentage
    );

    public static PlayerScoutMetrics CalculateMetrics(
        PlayerPosition position,
        int age,
        int height,
        decimal weight,
        decimal marketValueAmount)
    {
        // 1. Piyasa Değeri Seviyesi (Base Rating 65 - 94 arası)
        int baseQuality;
        if (marketValueAmount >= 100_000_000) baseQuality = 93;
        else if (marketValueAmount >= 60_000_000) baseQuality = 88;
        else if (marketValueAmount >= 30_000_000) baseQuality = 84;
        else if (marketValueAmount >= 15_000_000) baseQuality = 80;
        else if (marketValueAmount >= 5_000_000)  baseQuality = 75;
        else if (marketValueAmount >= 1_000_000)  baseQuality = 71;
        else baseQuality = 66;

        // 2. Mevkiye göre yetenek profili ağırlıkları
        int pace = baseQuality;
        int shooting = baseQuality;
        int passing = baseQuality;
        int dribbling = baseQuality;
        int defending = baseQuality;
        int physicality = baseQuality;

        switch (position)
        {
            case PlayerPosition.ST:
                shooting += 6;
                pace += 4;
                dribbling += 3;
                defending -= 38;
                passing -= 5;
                break;

            case PlayerPosition.CM:
                passing += 7;
                dribbling += 5;
                defending -= 2;
                physicality += 1;
                shooting -= 2;
                pace -= 1;
                break;

            case PlayerPosition.CB:
                defending += 8;
                physicality += 7;
                shooting -= 35;
                dribbling -= 18;
                passing -= 8;
                pace -= 5;
                break;

            case PlayerPosition.GK:
                defending += 10; // Kalecilik refleksleri
                physicality += 3;
                pace -= 32;
                shooting -= 55;
                dribbling -= 45;
                passing -= 15;
                break;
        }

        // 3. Fiziksel etki
        if (height >= 188)
        {
            physicality += 4;
            pace -= 3;
        }
        else if (height > 0 && height <= 175)
        {
            pace += 4;
            dribbling += 3;
            physicality -= 3;
        }

        // 4. Yaş & Wonderkid etki faktörü
        bool isWonderkid = age <= 21;
        decimal potentialGrowth = age switch
        {
            <= 19 => 28.5m,
            <= 21 => 22.0m,
            <= 23 => 14.0m,
            <= 26 => 6.5m,
            <= 29 => 1.0m,
            _ => -8.0m
        };

        if (isWonderkid)
        {
            pace += 3;
            physicality -= 2; // Gelişmekte olan fizik
        }

        // Sınırlandırma (Clamp 30 - 99)
        pace = Math.Clamp(pace, 30, 99);
        shooting = Math.Clamp(shooting, 25, 99);
        passing = Math.Clamp(passing, 30, 99);
        dribbling = Math.Clamp(dribbling, 25, 99);
        defending = Math.Clamp(defending, 20, 99);
        physicality = Math.Clamp(physicality, 35, 99);

        // Genel Scout Skoru (Ağırlıklı Ortalama)
        int overallScore = (int)Math.Round((pace * 0.18) + (shooting * 0.18) + (passing * 0.18) + (dribbling * 0.18) + (defending * 0.14) + (physicality * 0.14));
        if (position == PlayerPosition.GK)
        {
            overallScore = (int)Math.Round((defending * 0.6) + (physicality * 0.25) + (passing * 0.15));
        }
        overallScore = Math.Clamp(overallScore, 40, 99);

        string tier = overallScore switch
        {
            >= 90 => "Dünya Yıldızı (World Class)",
            >= 84 => "Elit Futbolcu (Elite)",
            >= 78 => "Süper Lig Kalitesi (First Team)",
            >= 72 => "Potansiyelli Yetenek (Prospect)",
            _ => "Gelişim Seviyesi (Developing)"
        };

        return new PlayerScoutMetrics(
            Pace: pace,
            Shooting: shooting,
            Passing: passing,
            Dribbling: dribbling,
            Defending: defending,
            Physicality: physicality,
            ScoutRating: overallScore,
            IsWonderkid: isWonderkid,
            ScoutTier: tier,
            PotentialGrowthPercentage: potentialGrowth
        );
    }
}
