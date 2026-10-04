shader_type canvas_item;
render_mode unshaded;

// 0 = Hidden, 1 = Active, 2 = Smoldering
uniform int state : hint_range(0, 2) = 1;

// Активное — лампа
uniform vec4 core_color_active : source_color = vec4(1.00, 0.95, 0.80, 1.0);
uniform vec4 body_color_active : source_color = vec4(1.00, 0.25, 0.20, 1.0);
uniform vec4 halo_color_active : source_color = vec4(1.00, 0.10, 0.05, 1.0);
uniform float flicker_speed_active : hint_range(0.0, 20.0) = 5.0;
uniform float flicker_amp_active   : hint_range(0.0, 1.0)  = 0.20;

// Тлеющая лучина
uniform vec4 core_color_idle : source_color = vec4(0.55, 0.30, 0.15, 1.0);
uniform vec4 body_color_idle : source_color = vec4(0.30, 0.12, 0.08, 1.0);
uniform vec4 halo_color_idle : source_color = vec4(0.20, 0.05, 0.03, 1.0);
uniform float flicker_speed_idle : hint_range(0.0, 20.0) = 1.2;
uniform float flicker_amp_idle   : hint_range(0.0, 1.0)  = 0.08;

void fragment() {
    // Активен ли режим лампы (state == 1)
    float is_active = (state == 1) ? 1.0 : 0.0;

    // Параметры текущего состояния
    vec4  core_color = mix(core_color_idle, core_color_active, is_active);
    vec4  body_color = mix(body_color_idle, body_color_active, is_active);
    vec4  halo_color = mix(halo_color_idle, halo_color_active, is_active);
    float f_speed    = mix(flicker_speed_idle, flicker_speed_active, is_active);
    float f_amp      = mix(flicker_amp_idle,   flicker_amp_active,   is_active);

    // Мерцание
    float f = 1.0 + f_amp * sin(TIME * f_speed);

    // Вертикаль: низ — тело, верх — нить
    float vertical = UV.y;
    vec3 rgb = mix(body_color.rgb, core_color.rgb, 1.0 - vertical);

    float top_boost = mix(0.7, 1.0, is_active);   // было 0.4
	rgb = mix(rgb, vec3(top_boost), step(vertical, 0.15));

	// Ореол по бокам
	float edge_x = min(UV.x, 1.0 - UV.x);
	float halo = smoothstep(0.3, 0.0, edge_x) * mix(0.3, 0.4, is_active);   // было 0.15
	rgb = mix(rgb, halo_color.rgb, halo);

	// Мерцание
	rgb *= f;

	// Прозрачность
	float alpha;
	if (state == 0)
	    alpha = 0.0;
	else if (state == 2)
	    alpha = 0.85;   // было 0.55
	else
	    alpha = 1.0;

	COLOR = vec4(rgb, alpha);
}