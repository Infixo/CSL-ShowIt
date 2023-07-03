// Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// ImmaterialResourceManager
using ColossalFramework;
using ColossalFramework.Math;
using ICities;
using UnityEngine;

private static bool CalculateLocalResources(int x, int z, ushort[] buffer, int[] global, ushort[] target, int index)
{
	int num1 = buffer[index] + global[0]; // HealthCare = 0,
	int num2 = buffer[index + 1] + global[1]; // FireDepartment = 1,
	int num3 = buffer[index + 2] + global[2]; // PoliceDepartment = 2,
	int num4 = buffer[index + 3] + global[3]; // EducationElementary = 3,
	int num5 = buffer[index + 4] + global[4]; // EducationHighSchool = 4,
	int num6 = buffer[index + 5] + global[5]; // EducationUniversity = 5,
	int num7 = buffer[index + 6] + global[6]; // DeathCare = 6,
	int num8 = buffer[index + 7] + global[7]; // PublicTransport = 7,
	int num9 = buffer[index + 8] + global[8]; // NoisePollution = 8,
	int num10 = buffer[index + 9] + global[9]; // CrimeRate = 9,
	int num11 = buffer[index + 10] + global[10]; // Health = 10,
	int num12 = buffer[index + 11] + global[11]; // Wellbeing = 11,
	int num13 = buffer[index + 12] + global[12]; // Density = 12 - intermediate
	int num14 = buffer[index + 13] + global[13]; // Entertainment = 13,
	int landVal = buffer[index + 14] + global[14]; // LandValue = 14
	//int num16 = buffer[index + 15]; // Attractiveness = 1 - NOT USED
	//int num17 = buffer[index + 16] + global[16]; // Coverage = 16 - NOT USED
	int num18 = buffer[index + 17] + global[17]; // FireHazard = 17,
	int num19 = buffer[index + 18] + global[18]; // Abandonment = 18,
	int num20 = buffer[index + 19] + global[19]; // CargoTransport = 19,
	int num21 = buffer[index + 20] + global[20]; // RadioCoverage = 20,
	int num22 = buffer[index + 21]; // FirewatchCoverage = 21,
	//int value = buffer[index + 22] + global[22]; // EarthquakeCoverage = 22, NOT USED
	int num23 = buffer[index + 23] + global[23]; // DisasterCoverage = 23,
	//int value2 = buffer[index + 24] + global[24]; // TourCoverage = 24, NOT USED
	int num24 = buffer[index + 25] + global[25]; // PostService = 25,
	int num25 = buffer[index + 26] + global[26]; // EducationLibrary = 26,
	int num26 = buffer[index + 27] + global[27]; // ChildCare = 27,
	int num27 = buffer[index + 28] + global[28]; // ElderCare = 28,
	//int value3 = buffer[index + 29] + global[29]; // CashCollecting = 29, NOT USED
	//int value4 = buffer[index + 30] + global[30]; // TaxBonus = 30, NOT USED
	//int rate = buffer[index + 31] + global[31]; // Sightseeing = 31, NOT USED
	//int rate2 = buffer[index + 32] + global[32]; // Shopping = 32, NOT USED
	//int rate3 = buffer[index + 33] + global[33]; // Business = 33, NOT USED
	//int rate4 = buffer[index + 34] + global[34]; // Nature = 34, NOT USED
	Rect area = new Rect(((float)x - 128f - 1.5f) * 38.4f, ((float)z - 128f - 1.5f) * 38.4f, 153.6f, 153.6f); // LV
	Singleton<NaturalResourceManager>.instance.AveragePollutionAndWaterAndTrees(area, out var groundPollution, out var waterProximity, out var treeProximity); // LV
	int num28 = (int)(groundPollution * 100f); // LV
	int num29 = (int)(waterProximity * 100f); // LV
	int num30 = (int)(treeProximity * 100f); // LV
	if (num29 > 33 && num29 < 99) // LV
	{
		area = new Rect(((float)x - 128f + 0.25f) * 38.4f, ((float)z - 128f + 0.25f) * 38.4f, 19.2f, 19.2f);
		Singleton<NaturalResourceManager>.instance.AverageWater(area, out waterProximity);
		num29 = Mathf.Max(Mathf.Min(num29, (int)(waterProximity * 100f)), 33);
	}
	num18 = num18 * 2 / (num2 + 50); // LV
	num9 = (num9 * (100 - num30) + 50) / 100; // LV NoisePollution is adjusted by trees
	if (num13 == 0) // Density
	{
		num10 = 0;
		num11 = 50;
		num12 = 50;
	}
	else
	{
		num10 /= num13;
		num11 /= num13;
		num12 /= num13;
		num17 += Mathf.Min(num13, 10) * 10;
	}
	bool flag = Singleton<GameAreaManager>.instance.PointOutOfArea(VectorUtils.X_Y(area.center));
	if (flag || x <= 1 || x >= 254 || z <= 1 || z >= 254)
	{
		// this is out of bounds
		landVal = 0;
		num16 = 0;
		if (Singleton<LoadingManager>.instance.SupportsExpansion(Expansion.Hotels))
		{
			rate = 0;
			rate2 = 0;
			rate3 = 0;
			rate4 = 0;
		}
	}
	else
	{
		// this is actual calc
		int num31 = CalculateResourceEffect(num29, 33, 67, 300, 0) * Mathf.Max(0, 32 - num28) >> 5; // LV
		int num32 = CalculateResourceEffect(num30, 10, 100, 0, 30);
		// MAIN SECTION
		landVal += CalculateResourceEffect(num1, 100, 500, 50, 100); // HealthCare 10
		landVal += CalculateResourceEffect(num2, 100, 500, 50, 100); // FireDepartment 10
		landVal += CalculateResourceEffect(num3, 100, 500, 50, 100); // PoliceDepartment 10
		landVal += CalculateResourceEffect(num4, 100, 500, 50, 100); // EducationElementary 10
		landVal += CalculateResourceEffect(num5, 100, 500, 50, 100); // EducationHighSchool 10
		landVal += CalculateResourceEffect(num6, 100, 500, 50, 100); // EducationUniversity 10
		landVal += CalculateResourceEffect(num7, 100, 500, 50, 100); // DeathCare 10
		landVal += CalculateResourceEffect(num8, 100, 500, 50, 100); // PublicTransport 10
		landVal += CalculateResourceEffect(num20, 100, 500, 50, 100); // CargoTransport 10
		landVal += CalculateResourceEffect(num14, 100, 500, 100, 200); // Entertainment 20
		landVal += CalculateResourceEffect(num12, 60, 100, 0, 50); // Wellbeing 5 NEW
		landVal += CalculateResourceEffect(num11, 60, 100, 0, 50); // Health 5 NEW
		landVal += CalculateResourceEffect(num21, 50, 100, 20, 25); // RadioCoverage 2.5
		landVal += CalculateResourceEffect(num23, 50, 100, 20, 25); // DisasterCoverage 2.5
		landVal += CalculateResourceEffect(num22, 100, 1000, 0, 25); // FirewatchCoverage 2.5
		landVal += CalculateResourceEffect(num24, 100, 200, 20, 30); // PostService 3
		landVal += CalculateResourceEffect(num25, 100, 500, 50, 200); // EducationLibrary 20 NEW
		landVal += CalculateResourceEffect(num26, 100, 500, 50, 100); // ChildCare 10 NEW
		landVal += CalculateResourceEffect(num27, 100, 500, 50, 100); // ElderCare 10 NEW
		landVal -= CalculateResourceEffect(100 - num12, 60, 100, 0, 50); // Wellbeing -5 NEW
		landVal -= CalculateResourceEffect(100 - num11, 60, 100, 0, 50); // Health -5 NEW
		landVal -= CalculateResourceEffect(num28, 50, 255, 50, 100); // ground pollution -10
		landVal -= CalculateResourceEffect(num9, 10, 100, 0, 100); // NoisePollution -10
		landVal -= CalculateResourceEffect(num10, 10, 100, 0, 100); // CrimeRate -10 NEW
		landVal -= CalculateResourceEffect(num18, 50, 100, 10, 50); // FireHazard -5 NEW
		landVal -= CalculateResourceEffect(num19, 15, 50, 100, 200); // Abandonment -20
		landVal += num31;
		landVal /= 10;
		// MAIN SECTION
		num16 += num31 * 25 / 300;
		num16 += num32;
		if (Singleton<LoadingManager>.instance.SupportsExpansion(Expansion.Hotels))
		{
			Rect area2 = new Rect(((float)x - 128f - 1.5f) * 38.4f, ((float)z - 128f - 1.5f) * 38.4f, 153.6f, 153.6f);
			int wind = (int)Singleton<WeatherManager>.instance.AverageWind(area2);
			CalculateHotelLocalResources(ref rate, ref Singleton<ImmaterialResourceManager>.instance.m_properties.m_hotel.m_sightseeingHotel, num28, num9, num10, num8, num30, num29, wind);
			CalculateHotelLocalResources(ref rate2, ref Singleton<ImmaterialResourceManager>.instance.m_properties.m_hotel.m_shoppingHotel, num28, num9, num10, num8, num30, num29, wind);
			CalculateHotelLocalResources(ref rate3, ref Singleton<ImmaterialResourceManager>.instance.m_properties.m_hotel.m_businessHotel, num28, num9, num10, num8, num30, num29, wind);
			CalculateHotelLocalResources(ref rate4, ref Singleton<ImmaterialResourceManager>.instance.m_properties.m_hotel.m_natureHotel, num28, num9, num10, num8, num30, num29, wind);
		}
	}
	num1 = Mathf.Clamp(num1, 0, 65535);
	num2 = Mathf.Clamp(num2, 0, 65535);
	num3 = Mathf.Clamp(num3, 0, 65535);
	num4 = Mathf.Clamp(num4, 0, 65535);
	num5 = Mathf.Clamp(num5, 0, 65535);
	num6 = Mathf.Clamp(num6, 0, 65535);
	num7 = Mathf.Clamp(num7, 0, 65535);
	num8 = Mathf.Clamp(num8, 0, 65535);
	num9 = Mathf.Clamp(num9, 0, 65535);
	num10 = Mathf.Clamp(num10, 0, 65535);
	num11 = Mathf.Clamp(num11, 0, 65535);
	num12 = Mathf.Clamp(num12, 0, 65535);
	num13 = Mathf.Clamp(num13, 0, 65535);
	num14 = Mathf.Clamp(num14, 0, 65535);
	landVal = Mathf.Clamp(landVal, 0, 65535); // LV
	num16 = Mathf.Clamp(num16, 0, 65535);
	num17 = Mathf.Clamp(num17, 0, 65535);
	num18 = Mathf.Clamp(num18, 0, 65535);
	num19 = Mathf.Clamp(num19, 0, 65535);
	num20 = Mathf.Clamp(num20, 0, 65535);
	num21 = Mathf.Clamp(num21, 0, 65535);
	num22 = Mathf.Clamp(num22, 0, 65535);
	value = Mathf.Clamp(value, 0, 65535);
	num23 = Mathf.Clamp(num23, 0, 65535);
	value2 = Mathf.Clamp(value2, 0, 65535);
	num24 = Mathf.Clamp(num24, 0, 65535);
	num25 = Mathf.Clamp(num25, 0, 65535);
	num26 = Mathf.Clamp(num26, 0, 65535);
	num27 = Mathf.Clamp(num27, 0, 65535);
	value3 = Mathf.Clamp(value3, 0, 65535);
	value4 = Mathf.Clamp(value4, 0, 65535);
	rate = Mathf.Clamp(rate, 0, 65535);
	rate2 = Mathf.Clamp(rate2, 0, 65535);
	rate3 = Mathf.Clamp(rate3, 0, 65535);
	rate4 = Mathf.Clamp(rate4, 0, 65535);
	DistrictManager districtManager = Singleton<DistrictManager>.instance;
	byte district = districtManager.GetDistrict(x * 2, z * 2);
	districtManager.m_districts.m_buffer[district].AddGroundData(landVal, num28, num17); // LV - but only results
	bool result = false;
	if (num1 != target[index])
	{
		target[index] = (ushort)num1;
		result = true;
	}
	if (num2 != target[index + 1])
	{
		target[index + 1] = (ushort)num2;
		result = true;
	}
	if (num3 != target[index + 2])
	{
		target[index + 2] = (ushort)num3;
		result = true;
	}
	if (num4 != target[index + 3])
	{
		target[index + 3] = (ushort)num4;
		result = true;
	}
	if (num5 != target[index + 4])
	{
		target[index + 4] = (ushort)num5;
		result = true;
	}
	if (num6 != target[index + 5])
	{
		target[index + 5] = (ushort)num6;
		result = true;
	}
	if (num7 != target[index + 6])
	{
		target[index + 6] = (ushort)num7;
		result = true;
	}
	if (num24 != target[index + 25])
	{
		target[index + 25] = (ushort)num24;
		result = true;
	}
	if (num8 != target[index + 7])
	{
		target[index + 7] = (ushort)num8;
		result = true;
	}
	if (num9 != target[index + 8])
	{
		target[index + 8] = (ushort)num9;
		result = true;
	}
	if (num10 != target[index + 9])
	{
		target[index + 9] = (ushort)num10;
		result = true;
	}
	if (num11 != target[index + 10])
	{
		target[index + 10] = (ushort)num11;
		result = true;
	}
	if (num12 != target[index + 11])
	{
		target[index + 11] = (ushort)num12;
		result = true;
	}
	if (num13 != target[index + 12])
	{
		target[index + 12] = (ushort)num13;
		result = true;
	}
	if (num14 != target[index + 13])
	{
		target[index + 13] = (ushort)num14;
		result = true;
	}
	if (landVal != target[index + 14]) // LandValue = 14
	{
		target[index + 14] = (ushort)landVal; // LandValue = 14
		result = true;
	}
	if (num16 != target[index + 15])
	{
		target[index + 15] = (ushort)num16;
		result = true;
	}
	if (num17 != target[index + 16])
	{
		target[index + 16] = (ushort)num17;
		result = true;
	}
	if (num18 != target[index + 17])
	{
		target[index + 17] = (ushort)num18;
		result = true;
	}
	if (num19 != target[index + 18])
	{
		target[index + 18] = (ushort)num19;
		result = true;
	}
	if (num20 != target[index + 19])
	{
		target[index + 19] = (ushort)num20;
		result = true;
	}
	if (num21 != target[index + 20])
	{
		target[index + 20] = (ushort)num21;
		result = true;
	}
	if (num22 != target[index + 21])
	{
		target[index + 21] = (ushort)num22;
		result = true;
	}
	if (value != target[index + 22])
	{
		target[index + 22] = (ushort)value;
		result = true;
	}
	if (num23 != target[index + 23])
	{
		target[index + 23] = (ushort)num23;
		result = true;
	}
	if (value2 != target[index + 24])
	{
		target[index + 24] = (ushort)value2;
		result = true;
	}
	if (num25 != target[index + 26])
	{
		target[index + 26] = (ushort)num25;
		result = true;
	}
	if (num26 != target[index + 27])
	{
		target[index + 27] = (ushort)num26;
		result = true;
	}
	if (num27 != target[index + 28])
	{
		target[index + 28] = (ushort)num27;
		result = true;
	}
	if (value3 != target[index + 29])
	{
		target[index + 29] = (ushort)value3;
		result = true;
	}
	if (value4 != target[index + 30])
	{
		target[index + 30] = (ushort)value4;
		result = true;
	}
	if (rate != target[index + 31])
	{
		target[index + 31] = (ushort)rate;
		result = true;
	}
	if (rate2 != target[index + 32])
	{
		target[index + 32] = (ushort)rate2;
		result = true;
	}
	if (rate3 != target[index + 33])
	{
		target[index + 33] = (ushort)rate3;
		result = true;
	}
	if (rate4 != target[index + 34])
	{
		target[index + 34] = (ushort)rate4;
		result = true;
	}
	return result;
}
