CREATE TABLE IF NOT EXISTS public.to_do_list_item
(
    user_id bigint NOT NULL,
    item_id bigint NOT NULL,
    title character varying(200) COLLATE pg_catalog."default" NOT NULL,
    is_done boolean NOT NULL,
    CONSTRAINT to_do_list_item_pkey PRIMARY KEY (item_id, user_id)
)