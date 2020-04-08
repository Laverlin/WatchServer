--
-- PostgreSQL database dump
--

-- Dumped from database version 12.2
-- Dumped by pg_dump version 12.0

-- Started on 2020-04-04 23:03:57

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- TOC entry 202 (class 1259 OID 16880)
-- Name: deviceinfo_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.deviceinfo_id_seq
    START WITH 7
    INCREMENT BY 1
    NO MINVALUE
    MAXVALUE 2147483647
    CACHE 1;


SET default_table_access_method = heap;

--
-- TOC entry 203 (class 1259 OID 16882)
-- Name: DeviceInfo; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."DeviceInfo" (
    id integer DEFAULT nextval('public.deviceinfo_id_seq'::regclass) NOT NULL,
    "DeviceId" character varying NOT NULL,
    "DeviceName" character varying,
    "FirstRequestTime" timestamp with time zone DEFAULT now()
);



--
-- TOC entry 225 (class 1255 OID 16891)
-- Name: add_device(character varying, character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.add_device(device_id character varying, device_name character varying) RETURNS TABLE(id integer, "DeviceId" character varying, "DeviceName" character varying)
    LANGUAGE sql
    AS $$

WITH input_rows("DeviceId", "DeviceName") AS (SELECT device_id, device_name)  -- see above
, ins AS (
   INSERT INTO "DeviceInfo" AS D ("DeviceId", "DeviceName") 
   SELECT I."DeviceId", I."DeviceName" FROM input_rows I
   ON CONFLICT ("DeviceId") DO NOTHING
--SET "DeviceName" = EXCLUDED."DeviceName"
   RETURNING id, "DeviceId", "DeviceName"                   -- we need unique columns for later join
   )
, sel AS (
   SELECT id, "DeviceId", ins."DeviceName"
   FROM   ins
   UNION  ALL
   SELECT id, "DeviceId", I."DeviceName"
   FROM   input_rows I
   JOIN   "DeviceInfo" D USING ("DeviceId")
   )
, ups AS (                                      -- RARE corner case
   INSERT INTO "DeviceInfo" AS D ("DeviceId", "DeviceName")  -- another UPSERT, not just UPDATE
   SELECT I."DeviceId", I."DeviceName"
   FROM   input_rows I
   LEFT   JOIN sel S USING ("DeviceId")     -- columns of unique index
   WHERE  S."DeviceId" IS NULL                         -- missing!
   ON     CONFLICT ("DeviceId") DO UPDATE     -- we've asked nicely the 1st time ...
   SET   "DeviceId" = D."DeviceId"                          -- ... this time we overwrite with old value
      --"DeviceName" = EXCLUDED."DeviceName"                  -- alternatively overwrite with *new* value
   RETURNING id, "DeviceId", "DeviceName"  --, usr, contact               -- return more columns?
   )
SELECT sel.id, sel."DeviceId", "DeviceName" FROM sel
UNION  ALL
TABLE  ups;

$$;


--
-- TOC entry 204 (class 1259 OID 16892)
-- Name: cityrequest_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.cityrequest_id_seq
    START WITH 14
    INCREMENT BY 1
    NO MINVALUE
    MAXVALUE 2147483647
    CACHE 1;


--
-- TOC entry 205 (class 1259 OID 16894)
-- Name: CityInfo; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."CityInfo" (
    id integer DEFAULT nextval('public.cityrequest_id_seq'::regclass) NOT NULL,
    "DeviceInfoId" integer,
    "CityName" character varying,
    "Lat" numeric(18,12),
    "Lon" numeric(18,12),
    "FaceVersion" character varying,
    "FrameworkVersion" character varying,
    "CIQVersion" character varying,
    "RequestTime" timestamp(4) without time zone,
    "PrecipProbability" numeric,
    "RequestType" character varying,
    "Temperature" numeric,
    "Wind" numeric,
    "BaseCurrency" character varying,
    "TargetCurrency" character varying,
    "ExchangeRate" numeric
);


--
-- TOC entry 206 (class 1259 OID 16901)
-- Name: yas_route; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.yas_route (
    route_id bigint NOT NULL,
    user_id bigint NOT NULL,
    route_name character varying,
    upload_time timestamp with time zone
);


--
-- TOC entry 207 (class 1259 OID 16907)
-- Name: yas_route_route_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.yas_route ALTER COLUMN route_id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.yas_route_route_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- TOC entry 208 (class 1259 OID 16909)
-- Name: yas_user; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.yas_user (
    user_id bigint NOT NULL,
    public_id character varying NOT NULL,
    telegram_id bigint NOT NULL,
    user_name character varying,
    register_time timestamp with time zone
);


--
-- TOC entry 209 (class 1259 OID 16915)
-- Name: yas_user_user_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.yas_user ALTER COLUMN user_id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.yas_user_user_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- TOC entry 210 (class 1259 OID 16917)
-- Name: yas_waypoint; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.yas_waypoint (
    waypoint_id bigint NOT NULL,
    route_id bigint NOT NULL,
    waypoint_name character varying,
    lat numeric,
    lon numeric,
    order_id integer
);


--
-- TOC entry 211 (class 1259 OID 16923)
-- Name: yas_waypoint_waypoint_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.yas_waypoint ALTER COLUMN waypoint_id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.yas_waypoint_waypoint_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- TOC entry 2828 (class 2606 OID 16926)
-- Name: DeviceInfo deviceinfo_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."DeviceInfo"
    ADD CONSTRAINT deviceinfo_pkey PRIMARY KEY (id);


--
-- TOC entry 2829 (class 1259 OID 16927)
-- Name: FK_DeviceInfoID; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "FK_DeviceInfoID" ON public."CityInfo" USING btree ("DeviceInfoId");

ALTER TABLE public."CityInfo" CLUSTER ON "FK_DeviceInfoID";


--
-- TOC entry 2826 (class 1259 OID 16928)
-- Name: IXU_DeviceID; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IXU_DeviceID" ON public."DeviceInfo" USING btree ("DeviceId");


--
-- TOC entry 2830 (class 1259 OID 16929)
-- Name: IX_RequestTime; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_RequestTime" ON public."CityInfo" USING btree ("RequestTime" DESC NULLS LAST);


--
-- TOC entry 2836 (class 1259 OID 16930)
-- Name: ix_waypoint_routeid; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_waypoint_routeid ON public.yas_waypoint USING btree (route_id);

ALTER TABLE public.yas_waypoint CLUSTER ON ix_waypoint_routeid;


--
-- TOC entry 2833 (class 1259 OID 16931)
-- Name: ixu_publicid; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX ixu_publicid ON public.yas_user USING btree (public_id);

ALTER TABLE public.yas_user CLUSTER ON ixu_publicid;


--
-- TOC entry 2831 (class 1259 OID 16932)
-- Name: ixu_route_routeid; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX ixu_route_routeid ON public.yas_route USING btree (route_id);


--
-- TOC entry 2832 (class 1259 OID 16933)
-- Name: ixu_route_userid; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ixu_route_userid ON public.yas_route USING btree (user_id);

ALTER TABLE public.yas_route CLUSTER ON ixu_route_userid;


--
-- TOC entry 2834 (class 1259 OID 16934)
-- Name: ixu_telegramid; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX ixu_telegramid ON public.yas_user USING btree (telegram_id);


--
-- TOC entry 2835 (class 1259 OID 16935)
-- Name: ixu_userid; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX ixu_userid ON public.yas_user USING btree (user_id);


--
-- TOC entry 2837 (class 1259 OID 16936)
-- Name: ixu_waypointid; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ixu_waypointid ON public.yas_waypoint USING btree (waypoint_id);


-- Completed on 2020-04-04 23:03:57

--
-- PostgreSQL database dump complete
--

