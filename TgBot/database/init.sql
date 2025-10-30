-- Создание последовательностей для автоинкремента
CREATE SEQUENCE IF NOT EXISTS public.list_id_list_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

CREATE SEQUENCE IF NOT EXISTS public.item_id_item_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

CREATE SEQUENCE IF NOT EXISTS public.reminder_id_reminder_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

-- Создание таблицы users
CREATE TABLE IF NOT EXISTS public.users
(
    id_user bigint NOT NULL,
    name character varying(60) COLLATE pg_catalog."default" NOT NULL,
    CONSTRAINT users_pkey PRIMARY KEY (id_user)
);

-- Создание таблицы list
CREATE TABLE IF NOT EXISTS public.list
(
    id_list bigint NOT NULL DEFAULT nextval('public.list_id_list_seq'::regclass),
    title_list character varying(60) COLLATE pg_catalog."default" NOT NULL,
    user_list bigint NOT NULL,
    CONSTRAINT list_pkey PRIMARY KEY (id_list),
    CONSTRAINT list_user_list_fkey FOREIGN KEY (user_list)
        REFERENCES public.users (id_user) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
);

-- Создание таблицы item_status
CREATE TABLE IF NOT EXISTS public.item_status
(
    id_status bigint NOT NULL,
    title_status character varying(15) COLLATE pg_catalog."default" NOT NULL,
    CONSTRAINT item_status_pkey PRIMARY KEY (id_status)
);

-- Создание таблицы item
CREATE TABLE IF NOT EXISTS public.item
(
    id_item bigint NOT NULL DEFAULT nextval('public.item_id_item_seq'::regclass),
    title_item character varying(200) COLLATE pg_catalog."default" NOT NULL,
    status_item bigint NOT NULL,
    list_item bigint NOT NULL,
    date_item date,
    time_item time without time zone,
    CONSTRAINT item_pkey PRIMARY KEY (id_item),
    CONSTRAINT item_list_item_fkey FOREIGN KEY (list_item)
        REFERENCES public.list (id_list) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT item_status_item_fkey FOREIGN KEY (status_item)
        REFERENCES public.item_status (id_status) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
);

-- Создание таблицы reminder
CREATE TABLE IF NOT EXISTS public.reminder
(
    id_reminder bigint NOT NULL DEFAULT nextval('public.reminder_id_reminder_seq'::regclass),
    item_reminder bigint NOT NULL,
    date_reminder date NOT NULL,
    time_reminder time without time zone NOT NULL,
    CONSTRAINT reminder_pkey PRIMARY KEY (id_reminder),
    CONSTRAINT reminder_item_reminder_fkey FOREIGN KEY (item_reminder)
        REFERENCES public.item (id_item) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
);

-- Добавление обязательных статусов задач
INSERT INTO public.item_status (id_status, title_status) VALUES 
(1, 'Запланировано'),
(2, 'Выполняется'),
(3, 'Выполнено');