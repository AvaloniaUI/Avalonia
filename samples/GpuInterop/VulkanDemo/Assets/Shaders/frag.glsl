#version 450
	layout(location = 0) in vec3 FragPos; 
	layout(location = 1) in vec3 VecPos; 
	layout(location = 2) in vec3 Normal;
	layout(push_constant) uniform constants{
    	layout(offset = 0) float maxY;
    	layout(offset = 4) float minY;
    	layout(offset = 8) float time;
    	layout(offset = 12) float disco;
    };
	layout(location = 0) out vec4 outFragColor;

	void main()
	{
		float y = (VecPos.y - minY) / (maxY - minY);
		float c = cos(atan(VecPos.x, VecPos.z) * 20.0 + time * 40.0 + y * 50.0);
		float s = sin(-atan(VecPos.z, VecPos.x) * 20.0 - time * 20.0 - y * 30.0);

		vec3 discoColor = vec3(
			0.5 + abs(0.5 - y) * cos(time * 10.0),
			0.25 + (smoothstep(0.3, 0.8, y) * (0.5 - c / 4.0)),
			0.25 + abs((smoothstep(0.1, 0.4, y) * (0.5 - s / 4.0))));

		vec3 objectColor = vec3((1.0 - y), 0.40 +  y / 4.0, y * 0.75 + 0.25);
		objectColor = objectColor * (1.0 - disco) + discoColor * disco;

		float ambientStrength = 0.3;
		vec3 lightColor = vec3(1.0, 1.0, 1.0);
		vec3 lightPos = vec3(maxY * 2.0, maxY * 2.0, maxY * 2.0);
		vec3 ambient = ambientStrength * lightColor;


		vec3 norm = normalize(Normal);
		vec3 lightDir = normalize(lightPos - FragPos);  

		float diff = max(dot(norm, lightDir), 0.0);
		vec3 diffuse = diff * lightColor;

		vec3 result = (ambient + diffuse) * objectColor;
		outFragColor = vec4(result, 1.0);

	}