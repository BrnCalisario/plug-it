using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Reddit.Controllers;

using Model;
using Repositories;
using DTO;
using Services;

[ApiController]
[EnableCors("MainPolicy")]
[Route("role")]
public class RoleController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult> AddRole(
        [FromBody] RoleDTO roleData,
        [FromServices] IGroupRepository groupRepository,
        [FromServices] IRoleRepository roleRepository,
        [FromServices] IUserService userService
    )
    {
        User user;
        try
        {
            user = await userService.ValidateUserToken(new Jwt { Value = roleData.Jwt });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }

        if (user is null)
            return NotFound();

        Role role = new Role
        {
            Name = roleData.Name,
            GroupId = roleData.GroupId
        };

        await roleRepository.InsertRole(role, roleData.PermissionsSet);

        return Ok();
    }

    [HttpPut]
    public async Task<ActionResult> UpdateRole(
        [FromBody] RoleDTO roleData,
        [FromServices] IGroupRepository groupRepository,
        [FromServices] IRoleRepository roleRepository,
        [FromServices] IUserService userService
    )
    {
        if (roleData.Id == 0)
            return BadRequest();

        User user;
        try
        {
            user = await userService.ValidateUserToken(new Jwt { Value = roleData.Jwt });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }

        if (user is null)
            return NotFound();

        Role role = await roleRepository.Find(roleData.Id);

        await roleRepository.UpdateRole(role, roleData.PermissionsSet);

        return Ok();
    }

    [HttpDelete]
    public async Task<ActionResult> DeleteRole(
        [FromBody] RoleDTO roleData,
        [FromServices] IGroupRepository groupRepository,
        [FromServices] IRoleRepository roleRepository,
        [FromServices] IUserService userService
    )
    {
        if (roleData.Id == 0)
            return BadRequest();

        User user;
        try
        {
            user = await userService.ValidateUserToken(new Jwt { Value = roleData.Jwt });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }

        if (user is null)
            return NotFound();

        Role role = await roleRepository.Find(roleData.Id);

        if(role is null)
            return NotFound("Not found role");

        Group group = role.Group;

        bool canManage = await groupRepository.HasPermission(user, group, PermissionEnum.ManageRole);

        if(!canManage)
            return BadRequest();

        await roleRepository.DeleteRole(role);

        return Ok();
    }

}