using IRM.SignalRTZ.Common.Models;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IRM.SignalRTZ.DataAccess.Repositories
{
    public interface ISystemReportRepository
    {
        Task<bool> StoreSystemReport(string address, RAMInfo ramInfo, CPUInfo cpuInfo, DisksInfo disksInfo);
        Task<bool> UpdateDelay(int seconds);
        Task<List<ReportModel>> GetCurrentReports();
        Task<int?> GetDelay();
    }

    public class SystemReportRepository : BaseRepository, ISystemReportRepository
    {
        private string _conn;

        public SystemReportRepository(string connectionString)
        {
            _conn = connectionString;
        }

        /// <summary>
        /// Возвращает период обовления в секундах
        /// </summary>
        public async Task<int?> GetDelay()
        {
            int? delay;

            try
            {
                using (NpgsqlConnection conn = GetConnection(_conn))
                {
                    using (var cmd = new NpgsqlCommand("select value from IRM_Configuration where key = 'UpdateRateSeconds'", conn))
                    {
                        await conn.OpenAsync();
                        delay = int.Parse((string)(await cmd.ExecuteScalarAsync()));
                        await conn.CloseAsync();
                    }
                }
            } catch (Exception e)
            {
                return null;
            }

            return delay;
        }

        /// <summary>
        /// Возвращает текущие (обновлённые не позже периода обновления) отчёты из бд
        /// </summary>
        public async Task<List<ReportModel>> GetCurrentReports()
        {
            var result = new List<ReportModel>();

            try
            {
                var delay = await GetDelay();
                if (!delay.HasValue || delay.Value <= 0)
                {
                    return null;
                }

                using (NpgsqlConnection conn = GetConnection(_conn))
                {
                    using (var cmd = new NpgsqlCommand(GetCurrentRecordsQuery(delay.Value), conn))
                    {
                        await conn.OpenAsync();
                        var reader = await cmd.ExecuteReaderAsync();
                        if (!reader.HasRows)
                        {
                            return result;
                        }

                        while (await reader.ReadAsync())
                        {
                            var row = new ReportModel
                            {
                                RamTotal = reader.GetInt32(reader.GetOrdinal("total_ram_mb")),
                                RamFree = reader.GetInt32(reader.GetOrdinal("free_ram_mb")),
                                DiskTotal = reader.GetInt32(reader.GetOrdinal("total_disk_mb")),
                                DiskFree = reader.GetInt32(reader.GetOrdinal("free_disk_mb")),
                                CpuUsage = reader.GetInt32(reader.GetOrdinal("cpu_load")),
                                Ip = reader.GetString(reader.GetOrdinal("address")),
                                UpdateDate = reader.GetDateTime(reader.GetOrdinal("update_date"))
                            };
                            result.Add(row);
                        }
                        await conn.CloseAsync();
                    }
                }
            }
            catch (Exception e)
            {
                return result;
            }

            return result;
        }

        /// <summary>
        /// Устанавливает период обновления
        /// </summary>
        /// <param name="seconds">Новый период обновления в секундах</param>
        public async Task<bool> UpdateDelay(int seconds)
        {
            if (seconds <= 0)
            {
                return false;
            }

            try
            {
                using (NpgsqlConnection conn = GetConnection(_conn))
                {
                    using (var cmd = new NpgsqlCommand("update IRM_Configuration set value = @p_value where key = 'UpdateRateSeconds'", conn))
                    {
                        cmd.Parameters.AddWithValue("p_value", seconds);
                        await conn.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();
                        await conn.CloseAsync();
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        /// <summary>
        /// добавляет в базу новый отчёт, либо обновляет уже имеющийся
        /// </summary>
        /// <param name="address">IP - адрес</param>
        /// <param name="ramInfo">информация об ОЗУ</param>
        /// <param name="cpuInfo">информация о ЦП</param>
        /// <param name="disksInfo">информация о дисках</param>
        public async Task<bool> StoreSystemReport(string address, RAMInfo ramInfo, CPUInfo cpuInfo, DisksInfo disksInfo)
        {
            var result = false;

            if (string.IsNullOrEmpty(address) || ramInfo == null || cpuInfo == null || disksInfo == null)
                return false;

            var id = 0;

            using (NpgsqlConnection conn = GetConnection(_conn))
            {
                using (var cmd = new NpgsqlCommand(GetIdQuery(), conn))
                {
                    cmd.Parameters.AddWithValue("p_address", address);

                    try // ищем id
                    {
                        await conn.OpenAsync();
                        var searchResult = await cmd.ExecuteReaderAsync();
                        if (searchResult.HasRows) 
                        {
                            await searchResult.ReadAsync();
                            id = (int)searchResult[0];
                        }
                        await conn.CloseAsync();
                    }
                    catch (Exception e)
                    {
                        return false;
                    }

                    cmd.Parameters.AddWithValue("p_total_ram", ramInfo.TotalMb);
                    cmd.Parameters.AddWithValue("p_free_ram", ramInfo.FreeMb);
                    cmd.Parameters.AddWithValue("p_cpu_load", cpuInfo.UsedPercent);
                    cmd.Parameters.AddWithValue("p_free_disks", disksInfo.FreeMb);
                    cmd.Parameters.AddWithValue("p_total_disks", disksInfo.TotalMb);

                    try
                    {
                        if (id > 0) // если уже есть в базе, то обновляем
                        {
                            cmd.Parameters.AddWithValue("p_id", id);
                            cmd.CommandText = GetUpdateSystemReportQuery();
                            await conn.OpenAsync();
                            await cmd.ExecuteNonQueryAsync();
                            await conn.CloseAsync();
                        }
                        else // иначе добавляем в бд
                        {
                            cmd.CommandText = GetInsertSystemReportQuery();
                            await conn.OpenAsync();
                            await cmd.ExecuteNonQueryAsync();
                            await conn.CloseAsync();
                        }
                        result = true;
                    }
                    catch (Exception e)
                    {
                        result = false;
                    }
                }
                await conn.CloseAsync();
            }

            return result;
        }

        #region queries
        private string GetCurrentRecordsQuery(int delay)
        {
            return $@"select 
total_ram_mb, free_ram_mb, total_disk_mb, free_disk_mb, cpu_load, address, update_date 
from IRM_SystemReportData
where update_date > (NOW() - INTERVAL '{delay} seconds')::timestamp";
        }

        private string GetIdQuery()
        {
            return "select id from IRM_SystemReportData where address = @p_address";
        }

        private string GetInsertSystemReportQuery()
        {
            return @"INSERT INTO IRM_SystemReportData 
(address, total_ram_mb, free_ram_mb, cpu_load, free_disk_mb, total_disk_mb, update_date) 
VALUES (@p_address, @p_total_ram, @p_free_ram, @p_cpu_load, @p_free_disks, @p_total_disks, NOW()::timestamp)";
        }

        private string GetUpdateSystemReportQuery()
        {
            return @"update IRM_SystemReportData 
set total_ram_mb = @p_total_ram, 
    free_ram_mb = @p_free_ram, 
    cpu_load = @p_cpu_load, 
    free_disk_mb = @p_free_disks,
    total_disk_mb = @p_total_disks,
    update_date = NOW()::timestamp
where id = @p_id";
        }
        #endregion
    }
}
