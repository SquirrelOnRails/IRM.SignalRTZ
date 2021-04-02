create table IRM_SystemReportData (
  id serial primary key,
  address varchar not null,
  total_ram_mb numeric not null,
  free_ram_mb numeric not null,
  cpu_load numeric not null,
  free_disk_mb numeric not null,
  total_disk_mb numeric not null,
  update_date timestamp not null
);

comment on table IRM_SystemReportData is 'Данные о состоянии систем';
comment on column IRM_SystemReportData.id is 'Идентификатор записи';
comment on column IRM_SystemReportData.address is 'IP системы клиента';
comment on column IRM_SystemReportData.total_ram_mb is 'Объём оперативной памяти (в мегабайтах)';
comment on column IRM_SystemReportData.free_ram_mb is 'Объём свободной оперативной памяти (в мегабайтах)';
comment on column IRM_SystemReportData.cpu_load is 'Загруженность процессора системы (в процентах)';
comment on column IRM_SystemReportData.free_disk_mb is 'Свободное место на дисках (в мегабайтах)';
comment on column IRM_SystemReportData.total_disk_mb is 'Полный объём дисков (в мегабайтах)';
comment on column IRM_SystemReportData.update_date is 'Дата обновления данных';
