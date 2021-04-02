create table IRM_Configuration (
  key varchar(20) not null,
  value varchar(100) not null
);

comment on table IRM_Configuration is 'Конфигурация взаимодействия клиентов и сервера';
comment on column IRM_Configuration.key is 'Параметр';
comment on column IRM_Configuration.value is 'Значение';