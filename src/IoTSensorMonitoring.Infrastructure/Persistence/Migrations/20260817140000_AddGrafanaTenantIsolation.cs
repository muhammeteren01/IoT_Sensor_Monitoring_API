using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IoTSensorMonitoring.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGrafanaTenantIsolation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "grafana_org_id",
                table: "companies",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION app_current_company_id()
                RETURNS uuid
                LANGUAGE plpgsql
                STABLE
                AS $$
                BEGIN
                  RETURN NULLIF(current_setting('app.company_id', true), '')::uuid;
                EXCEPTION
                  WHEN invalid_text_representation THEN
                    RETURN NULL;
                END;
                $$;

                DO $body$
                BEGIN
                  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'grafana_reader') THEN
                    CREATE ROLE grafana_reader NOLOGIN;
                  END IF;
                END
                $body$;

                DO $connect$
                BEGIN
                  EXECUTE format('GRANT CONNECT ON DATABASE %I TO grafana_reader', current_database());
                END
                $connect$;
                GRANT USAGE ON SCHEMA public TO grafana_reader;
                GRANT SELECT ON TABLE
                  companies,
                  facilities,
                  zones,
                  sensors,
                  sensor_measurements,
                  alert_rules,
                  alert_history,
                  maintenance_logs,
                  device_models
                TO grafana_reader;
                ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT ON TABLES TO grafana_reader;

                ALTER TABLE companies ENABLE ROW LEVEL SECURITY;
                ALTER TABLE facilities ENABLE ROW LEVEL SECURITY;
                ALTER TABLE zones ENABLE ROW LEVEL SECURITY;
                ALTER TABLE sensors ENABLE ROW LEVEL SECURITY;
                ALTER TABLE sensor_measurements ENABLE ROW LEVEL SECURITY;
                ALTER TABLE alert_rules ENABLE ROW LEVEL SECURITY;
                ALTER TABLE alert_history ENABLE ROW LEVEL SECURITY;
                ALTER TABLE maintenance_logs ENABLE ROW LEVEL SECURITY;
                ALTER TABLE device_models ENABLE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS grafana_companies_select ON companies;
                CREATE POLICY grafana_companies_select ON companies
                  FOR SELECT TO grafana_reader
                  USING (id = app_current_company_id());

                DROP POLICY IF EXISTS grafana_facilities_select ON facilities;
                CREATE POLICY grafana_facilities_select ON facilities
                  FOR SELECT TO grafana_reader
                  USING (company_id = app_current_company_id());

                DROP POLICY IF EXISTS grafana_device_models_select ON device_models;
                CREATE POLICY grafana_device_models_select ON device_models
                  FOR SELECT TO grafana_reader
                  USING (company_id = app_current_company_id());

                DROP POLICY IF EXISTS grafana_zones_select ON zones;
                CREATE POLICY grafana_zones_select ON zones
                  FOR SELECT TO grafana_reader
                  USING (
                    EXISTS (
                      SELECT 1
                      FROM facilities f
                      WHERE f.id = zones.facility_id
                        AND f.company_id = app_current_company_id()
                    )
                  );

                DROP POLICY IF EXISTS grafana_sensors_select ON sensors;
                CREATE POLICY grafana_sensors_select ON sensors
                  FOR SELECT TO grafana_reader
                  USING (
                    EXISTS (
                      SELECT 1
                      FROM zones z
                      JOIN facilities f ON f.id = z.facility_id
                      WHERE z.id = sensors.zone_id
                        AND f.company_id = app_current_company_id()
                    )
                  );

                DROP POLICY IF EXISTS grafana_measurements_select ON sensor_measurements;
                CREATE POLICY grafana_measurements_select ON sensor_measurements
                  FOR SELECT TO grafana_reader
                  USING (
                    EXISTS (
                      SELECT 1
                      FROM sensors s
                      JOIN zones z ON z.id = s.zone_id
                      JOIN facilities f ON f.id = z.facility_id
                      WHERE s.id = sensor_measurements.sensor_id
                        AND f.company_id = app_current_company_id()
                    )
                  );

                DROP POLICY IF EXISTS grafana_alert_rules_select ON alert_rules;
                CREATE POLICY grafana_alert_rules_select ON alert_rules
                  FOR SELECT TO grafana_reader
                  USING (
                    EXISTS (
                      SELECT 1
                      FROM sensors s
                      JOIN zones z ON z.id = s.zone_id
                      JOIN facilities f ON f.id = z.facility_id
                      WHERE s.id = alert_rules.sensor_id
                        AND f.company_id = app_current_company_id()
                    )
                  );

                DROP POLICY IF EXISTS grafana_alert_history_select ON alert_history;
                CREATE POLICY grafana_alert_history_select ON alert_history
                  FOR SELECT TO grafana_reader
                  USING (
                    EXISTS (
                      SELECT 1
                      FROM sensors s
                      JOIN zones z ON z.id = s.zone_id
                      JOIN facilities f ON f.id = z.facility_id
                      WHERE s.id = alert_history.sensor_id
                        AND f.company_id = app_current_company_id()
                    )
                  );

                DROP POLICY IF EXISTS grafana_maintenance_logs_select ON maintenance_logs;
                CREATE POLICY grafana_maintenance_logs_select ON maintenance_logs
                  FOR SELECT TO grafana_reader
                  USING (
                    EXISTS (
                      SELECT 1
                      FROM sensors s
                      JOIN zones z ON z.id = s.zone_id
                      JOIN facilities f ON f.id = z.facility_id
                      WHERE s.id = maintenance_logs.sensor_id
                        AND f.company_id = app_current_company_id()
                    )
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP POLICY IF EXISTS grafana_maintenance_logs_select ON maintenance_logs;
                DROP POLICY IF EXISTS grafana_alert_history_select ON alert_history;
                DROP POLICY IF EXISTS grafana_alert_rules_select ON alert_rules;
                DROP POLICY IF EXISTS grafana_measurements_select ON sensor_measurements;
                DROP POLICY IF EXISTS grafana_sensors_select ON sensors;
                DROP POLICY IF EXISTS grafana_zones_select ON zones;
                DROP POLICY IF EXISTS grafana_device_models_select ON device_models;
                DROP POLICY IF EXISTS grafana_facilities_select ON facilities;
                DROP POLICY IF EXISTS grafana_companies_select ON companies;

                ALTER TABLE maintenance_logs DISABLE ROW LEVEL SECURITY;
                ALTER TABLE alert_history DISABLE ROW LEVEL SECURITY;
                ALTER TABLE alert_rules DISABLE ROW LEVEL SECURITY;
                ALTER TABLE sensor_measurements DISABLE ROW LEVEL SECURITY;
                ALTER TABLE sensors DISABLE ROW LEVEL SECURITY;
                ALTER TABLE zones DISABLE ROW LEVEL SECURITY;
                ALTER TABLE device_models DISABLE ROW LEVEL SECURITY;
                ALTER TABLE facilities DISABLE ROW LEVEL SECURITY;
                ALTER TABLE companies DISABLE ROW LEVEL SECURITY;

                DO $body$
                DECLARE
                  r record;
                BEGIN
                  FOR r IN SELECT rolname FROM pg_roles WHERE rolname LIKE 'g_c_%'
                  LOOP
                    EXECUTE format('DROP ROLE IF EXISTS %I', r.rolname);
                  END LOOP;
                END
                $body$;

                REVOKE ALL ON TABLE
                  companies,
                  facilities,
                  zones,
                  sensors,
                  sensor_measurements,
                  alert_rules,
                  alert_history,
                  maintenance_logs,
                  device_models
                FROM grafana_reader;
                DROP ROLE IF EXISTS grafana_reader;
                DROP FUNCTION IF EXISTS app_current_company_id();
                """);

            migrationBuilder.DropColumn(
                name: "grafana_org_id",
                table: "companies");
        }
    }
}
