-- Solo catálogos locales; no inventar productos ni proveedores.
INSERT INTO categoria(nombre) VALUES ('Alimentos'), ('Bebidas'), ('Limpieza') ON CONFLICT(nombre) DO NOTHING;
INSERT INTO empaque(nombre) VALUES ('Unidad'), ('Caja'), ('Bolsa') ON CONFLICT(nombre) DO NOTHING;
